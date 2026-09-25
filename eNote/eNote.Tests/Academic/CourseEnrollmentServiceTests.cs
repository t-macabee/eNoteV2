using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class CourseEnrollmentServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EnrollAsync_CreatesPendingRequest_ForPublishedCourse()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.CreatedById);
    }

    [Fact]
    public async Task EnrollAsync_ReopensCanceledEnrollment_AsPending()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Canceled));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
    }

    [Fact]
    public async Task EnrollAsync_Reenroll_PreservesPaidUntil()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now, 30);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);
        var canceled = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, canceled.EnrollmentStatus);
        Assert.Equal(Now.AddDays(30), canceled.PaidUntil);

        await service.EnrollAsync(course.Id);
        var reactivated = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, reactivated.EnrollmentStatus);
        Assert.Equal(Now.AddDays(30), reactivated.PaidUntil);
    }

    [Fact]
    public async Task EnrollAsync_ReopensRejectedEnrollment_AsPending_AndClearsDecision()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var rejected = new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending);
        rejected.Transition(EnrollmentTrigger.Reject, userId: 300, now: Now, reason: "Popunjeno");
        context.Set<Enrollment>().Add(rejected);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
        Assert.Null(enrollment.DecidedById);
        Assert.Null(enrollment.DecidedAt);
        Assert.Null(enrollment.DecisionNote);
    }

    [Fact]
    public async Task EnrollAsync_IsNoOp_WhenPending()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Null(enrollment.UpdatedById);
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenCourseCompleted()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Completed));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<BusinessException>(() => service.EnrollAsync(course.Id));

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_CancelsPendingRequest()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
    }

    [Fact]
    public async Task UnenrollAsync_TreatsConcurrencyViolation_AsConflict()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateConcurrencyException("Concurrency conflict.")), student);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.UnenrollAsync(course.Id));

        Assert.Equal(Messages.ConcurrencyConflict, ex.Message);
    }

    [Fact]
    public async Task UnenrollAsync_CancelsActiveEnrollment()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
    }

    [Fact]
    public async Task UnenrollAsync_Throws_WhenOpenIntentInsideReuseWindow()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        context.Set<CoursePayment>().Add(new CoursePayment(enrollment.Id, "pi_open", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.UnenrollAsync(course.Id));

        Assert.Equal(Messages.UnenrollBlockedByPendingPayment, ex.Message);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_Succeeds_WhenIntentOlderThanReuseWindow()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var payment = new CoursePayment(enrollment.Id, "pi_old", 10000, "bam", PaymentStatus.RequiresAction);
        context.Set<CoursePayment>().Add(payment);
        await context.SaveChangesAsync();
        payment.CreatedAt = Now.AddHours(-1);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_Succeeds_WhenMembershipIsInactive()
    {

        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: false);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, enrollment.EnrollmentStatus);
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenMembershipIsInactive()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: false);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<BusinessException>(() => service.EnrollAsync(course.Id));
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenCourseUnpublished()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        course.SetPublishedStatus(false);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.EnrollAsync(course.Id));
        Assert.Empty(context.Set<Enrollment>());
    }

    private static ENoteContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(storeId: 1)) { ExplicitStoreId = 1 };
    }

    private static async Task<(Student Student, Course Course)> SeedStudentAndCourseAsync(ENoteContext context, bool hasActiveMembership)
    {
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        student.UpdateMembership(hasActiveMembership ? Now.AddDays(1) : Now.AddDays(-1));

        var instructor = new Instructor(appUserId: 200);
        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Theory", null, 10, null, null, instructor.Id);
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        return (student, course);
    }

    [Fact]
    public async Task EnrollAsync_TreatsDuplicateEnrollmentViolation_AsConflict()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.EnrollmentStudentIdCourseIdUniqueIndex}\"");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)), student);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.EnrollAsync(course.Id));

        Assert.Equal(Messages.AlreadyEnrolled, ex.Message);
    }

    private static CourseEnrollmentService CreateService(IAppDbContext context, Student student)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(
            context,
            new FixedClock(Now),
            currentUser,
            currentUser,
            NullLogger<CourseEnrollmentService>.Instance);
    }
}
