using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Contracts.Communication;
using eNote.Contracts.Enrollments;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Academic;

public sealed class InstructorEnrollmentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ApproveAsync_SetsActive_RecordsDecision_AndEnqueuesNotification()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (student, pending) = await AddPendingStudentAsync(harness);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.ApproveAsync(harness.Course.Id, pending.Id);

        Assert.Equal(EnrollmentStatus.Active, dto.EnrollmentStatus);
        Assert.Equal(Now, dto.DecidedAt);
        Assert.Equal($"Student {student.Id}", dto.StudentName);

        var stored = await harness.Context.Set<Enrollment>().SingleAsync(e => e.Id == pending.Id);
        Assert.Equal(EnrollmentStatus.Active, stored.EnrollmentStatus);
        Assert.Equal(harness.Instructor.AppUserId, stored.DecidedById);
        Assert.Equal(Now, stored.DecidedAt);

        var outbox = Assert.Single(await harness.Context.Set<NotificationOutbox>().ToListAsync());
        Assert.Equal(NotificationMessageTypes.EnrollmentStatusChanged, outbox.MessageType);
        var payload = JsonSerializer.Deserialize<EnrollmentStatusChanged>(outbox.PayloadJson, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(pending.Id, payload.EnrollmentId);
        Assert.Equal(student.AppUserId, payload.StudentUserId);
        Assert.Equal("Upis odobren", payload.Title);
        Assert.Contains(harness.Course.Name, payload.Body);
    }

    [Fact]
    public async Task ApproveAsync_Throws_ForOtherInstructorsCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (_, pending) = await AddPendingStudentAsync(harness);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<AuthorizationException>(() => service.ApproveAsync(harness.Course.Id, pending.Id));

        var stored = await harness.Context.Set<Enrollment>().SingleAsync(e => e.Id == pending.Id);
        Assert.Equal(EnrollmentStatus.Pending, stored.EnrollmentStatus);
        Assert.Empty(await harness.Context.Set<NotificationOutbox>().ToListAsync());
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenEnrollmentBelongsToAnotherCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherCourse = new Course("Violin 101", null, 50m, Now, Now.AddMonths(1), harness.Instructor.Id);
        harness.Context.Set<Course>().Add(otherCourse);
        await harness.Context.SaveChangesAsync();
        var (_, otherEnrollment) = await AddPendingStudentAsync(harness, otherCourse.Id);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveAsync(harness.Course.Id, otherEnrollment.Id));
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenNotPending()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var active = await harness.Context.Set<Enrollment>()
            .SingleAsync(e => e.StudentId == harness.Student.Id && e.CourseId == harness.Course.Id);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<BusinessException>(() => service.ApproveAsync(harness.Course.Id, active.Id));

        Assert.Empty(await harness.Context.Set<NotificationOutbox>().ToListAsync());
    }

    [Fact]
    public async Task RejectAsync_Throws_WithoutReason()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (_, pending) = await AddPendingStudentAsync(harness);
        var service = CreateService(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.RejectAsync(harness.Course.Id, pending.Id, new EnrollmentRejectRequest { Reason = null }));

        Assert.Equal("Razlog odbijanja je obavezan.", ex.Message);
        var stored = await harness.Context.Set<Enrollment>().SingleAsync(e => e.Id == pending.Id);
        Assert.Equal(EnrollmentStatus.Pending, stored.EnrollmentStatus);
        Assert.Empty(await harness.Context.Set<NotificationOutbox>().ToListAsync());
    }

    [Fact]
    public async Task RejectAsync_SetsRejected_AndNotificationBodyContainsReason()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (_, pending) = await AddPendingStudentAsync(harness);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.RejectAsync(harness.Course.Id, pending.Id, new EnrollmentRejectRequest { Reason = "  Popunjeno  " });

        Assert.Equal(EnrollmentStatus.Rejected, dto.EnrollmentStatus);
        Assert.Equal("Popunjeno", dto.DecisionNote);

        var outbox = Assert.Single(await harness.Context.Set<NotificationOutbox>().ToListAsync());
        var payload = JsonSerializer.Deserialize<EnrollmentStatusChanged>(outbox.PayloadJson, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("Upis odbijen", payload.Title);
        Assert.Contains("Popunjeno", payload.Body);
    }

    [Fact]
    public async Task CompleteAsync_SetsCompleted_ForActiveEnrollment()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var active = await harness.Context.Set<Enrollment>()
            .SingleAsync(e => e.StudentId == harness.Student.Id && e.CourseId == harness.Course.Id);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CompleteAsync(harness.Course.Id, active.Id);

        Assert.Equal(EnrollmentStatus.Completed, dto.EnrollmentStatus);

        var stored = await harness.Context.Set<Enrollment>().SingleAsync(e => e.Id == active.Id);
        Assert.Equal(EnrollmentStatus.Completed, stored.EnrollmentStatus);

        var outbox = Assert.Single(await harness.Context.Set<NotificationOutbox>().ToListAsync());
        var payload = JsonSerializer.Deserialize<EnrollmentStatusChanged>(outbox.PayloadJson, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("Kurs položen", payload.Title);
    }

    [Fact]
    public async Task CompleteAsync_Throws_WhenNotActive()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (_, pending) = await AddPendingStudentAsync(harness);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<BusinessException>(() => service.CompleteAsync(harness.Course.Id, pending.Id));

        Assert.Empty(await harness.Context.Set<NotificationOutbox>().ToListAsync());
    }

    [Fact]
    public async Task GetForCourseAsync_FiltersByStatus_AndResolvesNames()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (pendingStudent, pending) = await AddPendingStudentAsync(harness);
        var service = CreateService(harness.Context, harness.Instructor);

        var result = await service.GetForCourseAsync(harness.Course.Id, new CourseEnrollmentSearchObject { EnrollmentStatus = EnrollmentStatus.Pending });

        var item = Assert.Single(result.Items);
        Assert.Equal(pending.Id, item.Id);
        Assert.Equal(pendingStudent.Id, item.StudentId);
        Assert.Equal(EnrollmentStatus.Pending, item.EnrollmentStatus);
        Assert.Equal($"Student {pendingStudent.Id}", item.StudentName);
    }

    [Fact]
    public async Task ApproveAsync_TreatsConcurrencyViolation_AsConflict()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (_, pending) = await AddPendingStudentAsync(harness);
        var context = new ThrowingSaveDbContext(harness.Context, new DbUpdateConcurrencyException("Concurrency conflict."));
        var service = CreateService(context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.ApproveAsync(harness.Course.Id, pending.Id));

        Assert.Equal(Messages.ConcurrencyConflict, ex.Message);
    }

    private static async Task<(Student Student, Enrollment Enrollment)> AddPendingStudentAsync(AcademicHarness harness, int? courseId = null)
    {
        var student = new Student(60, Now);
        harness.Context.Set<Student>().Add(student);
        await harness.Context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, courseId ?? harness.Course.Id, EnrollmentStatus.Pending);
        harness.Context.Set<Enrollment>().Add(enrollment);
        await harness.Context.SaveChangesAsync();

        return (student, enrollment);
    }

    private static InstructorEnrollmentService CreateService(IAppDbContext context, Instructor instructor)
    {
        var currentUser = new StubCurrentActor(instructor: instructor, userId: instructor.AppUserId);
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: instructor));

        return new(
            context,
            new FixedClock(Now),
            currentUser,
            instructorAccess,
            new StubDisplayNameService(),
            new EnrollmentNotificationDispatcher(context, new FixedClock(Now)));
    }
}
