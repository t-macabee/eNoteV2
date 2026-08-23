using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class LectureServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_CreatesLecture_ForOwnedCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(new LectureCreateRequest
        {
            CourseId = harness.Course.Id,
            Name = "Second lesson",
            Location = "Room 2",
            Duration = 90,
            LectureTime = Now.AddDays(1),
            LectureType = LectureType.Practical,
            Capacity = 20
        });

        Assert.Equal("Second lesson", dto.Name);
        Assert.Equal(LectureStatus.Scheduled, dto.LectureStatus);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCourseNotOwned()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Lesson",
                Location = "Room",
                Duration = 60,
                LectureTime = Now.AddDays(1),
                LectureType = LectureType.Theoretical
            }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
        {
            Name = "Renamed lesson",
            Location = "Lab",
            Duration = 120,
            LectureTime = Now.AddDays(2),
            Capacity = 10
        });

        Assert.Equal("Renamed lesson", dto.Name);
        Assert.Equal(120, dto.Duration);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenLectureCancelled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel();
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
            {
                Name = "Renamed",
                Location = "Lab",
                Duration = 60,
                LectureTime = Now.AddDays(2)
            }));
    }

    [Fact]
    public async Task CancelAsync_CancelsOwnedLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CancelAsync(harness.Lecture.Id);

        Assert.True(dto.IsCancelled);
    }

    // Contract: §7.2 requires a notification on lecture cancellation (eNote.Application/Features/
    // Academic/Lectures/Services/LectureService.cs CancelAsync). Proves the dispatch fires for the
    // currently-enrolled student, addressed by their AppUser id (not their Student profile id).
    [Fact]
    public async Task CancelAsync_DispatchesCancelledNotification_ForEnrolledStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(harness.Lecture.Id);

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Equal(harness.Lecture.Id, call.LectureId);
        Assert.Equal(harness.Lecture.Name, call.LectureName);
        Assert.Equal([harness.Student.AppUserId], call.EnrolledStudentUserIds);
    }

    // Regression: a student whose enrollment is no longer Active must not be notified —
    // ForEnrolledStudent-style filtering (StudentEnrollmentExtensions.cs) is duplicated by hand in
    // LectureService.CancelAsync's own enrollment query, so this guards that query independently.
    [Fact]
    public async Task CancelAsync_ExcludesStudentsWithInactiveEnrollment_FromNotification()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var droppedStudent = new Student(60, Now);
        harness.Context.Set<Student>().Add(droppedStudent);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().Add(new Enrollment(droppedStudent.Id, harness.Course.Id, EnrollmentStatus.Canceled));
        await harness.Context.SaveChangesAsync();
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(harness.Lecture.Id);

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.DoesNotContain(droppedStudent.AppUserId, call.EnrolledStudentUserIds);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_WhenNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var stranger = new Student(77, Now);
        harness.Context.Set<Student>().Add(stranger);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: stranger));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(harness.Lecture.Id));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenLocationOverlaps_IgnoringCase()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Overlap",
                Location = "ROOM 1",
                Duration = 60,
                LectureTime = Now.AddMinutes(30),
                LectureType = LectureType.Practical
            }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenInstructorOverlaps_AtDifferentLocation()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Overlap elsewhere",
                Location = "Room 9",
                Duration = 60,
                LectureTime = Now.AddMinutes(30),
                LectureType = LectureType.Theoretical
            }));
    }

    [Fact]
    public async Task CreateAsync_AllowsBackToBackLectures()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(new LectureCreateRequest
        {
            CourseId = harness.Course.Id,
            Name = "Next lesson",
            Location = "Room 1",
            Duration = 60,
            LectureTime = Now.AddMinutes(60),
            LectureType = LectureType.Practical
        });

        Assert.Equal("Next lesson", dto.Name);
    }

    [Fact]
    public async Task CreateAsync_IgnoresCancelledLectures()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel();
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(new LectureCreateRequest
        {
            CourseId = harness.Course.Id,
            Name = "Replacement",
            Location = "Room 1",
            Duration = 60,
            LectureTime = Now.AddMinutes(30),
            LectureType = LectureType.Practical
        });

        Assert.Equal("Replacement", dto.Name);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenMovedIntoOverlap()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = new Lecture("Second lesson", "Room 2", 60, Now.AddDays(1), LectureType.Theoretical, null, harness.Course.Id);
        harness.Context.Set<Lecture>().Add(other);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(other.Id, new LectureUpdateRequest
            {
                Name = "Second lesson",
                Location = "Room 1",
                Duration = 60,
                LectureTime = Now.AddMinutes(30)
            }));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenCapacityBelowConfirmedCount()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
            {
                Name = "First lesson",
                Location = "Room 1",
                Duration = 60,
                LectureTime = Now,
                Capacity = 0
            }));
    }

    [Fact]
    public async Task UpdateAsync_CancelledCheck_PrecedesCapacityValidation()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel();
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
            {
                Name = "First lesson",
                Location = "Room 1",
                Duration = 60,
                LectureTime = Now,
                Capacity = 0
            }));
    }

    private static LectureService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null, ILectureNotificationDispatcher? notificationDispatcher = null) =>
        new(context,
            actor ?? new StubCurrentActor(instructor: instructor),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            notificationDispatcher ?? new NoOpLectureNotificationDispatcher(),
            NullLogger<LectureService>.Instance,
            TestMapper.Create());
}
