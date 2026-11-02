using eNote.Application.Common.Localization;
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
    public async Task UpdateAsync_ReturnsAttendeeCount()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
        {
            Name = "First lesson",
            Location = "Room 1",
            Duration = 60,
            LectureTime = Now
        });

        Assert.Equal(1, dto.AttendeeCount);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenLectureCancelled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Razlog otkazivanja", 1, Now);
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

        var dto = await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        Assert.True(dto.IsCancelled);
        Assert.Equal("Instrument je pokvaren", dto.CancellationReason);
        Assert.Equal(Now, dto.CancelledAt);
    }

    [Fact]
    public async Task CancelAsync_StoresReasonAndCancellingUser()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(userId: harness.Instructor.AppUserId));

        await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "  Instrument je pokvaren  " });

        var lecture = harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id);
        Assert.Equal(LectureStatus.Cancelled, lecture.LectureStatus);
        Assert.Equal("Instrument je pokvaren", lecture.CancellationReason);
        Assert.Equal(Now, lecture.CancelledAt);
        Assert.Equal(harness.Instructor.AppUserId, lecture.CancelledById);
    }

    [Fact]
    public async Task CancelAsync_Throws_WhenAlreadyCancelled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Razlog otkazivanja", 1, Now);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Drugi razlog" }));

        Assert.Equal(Messages.LectureCancelled, ex.Message);
    }

    [Fact]
    public async Task CancelAsync_Throws_WhenLectureHeld()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).MarkHeld();
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" }));

        Assert.Equal(Messages.LectureAlreadyHeld, ex.Message);
    }

    [Fact]
    public async Task CancelAsync_ReturnsAttendeeCount()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        Assert.Equal(1, dto.AttendeeCount);
    }

    [Fact]
    public async Task CancelAsync_DispatchesCancelledNotification_ForEnrolledStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Equal(harness.Lecture.Id, call.LectureId);
        Assert.Equal(harness.Lecture.Name, call.LectureName);
        Assert.Equal("Instrument je pokvaren", call.Reason);
        Assert.Equal([harness.Student.AppUserId], call.EnrolledStudentUserIds);
    }

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

        await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.DoesNotContain(droppedStudent.AppUserId, call.EnrolledStudentUserIds);
    }

    [Fact]
    public async Task CancelAsync_NotifiesCanceledStudentsWithPaidTime_ButNotExpiredOnes()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var course = new Course("Piano 201", null, 100m, Now, Now.AddMonths(1), harness.Instructor.Id);
        course.SetPublishedStatus(true);
        harness.Context.Set<Course>().Add(course);
        var active = new Student(60, Now);
        var paidCanceled = new Student(61, Now);
        var expiredCanceled = new Student(62, Now);
        harness.Context.Set<Student>().AddRange(active, paidCanceled, expiredCanceled);
        await harness.Context.SaveChangesAsync();
        var lecture = new Lecture("Piano lesson", "Room 4", 60, Now.AddDays(1), LectureType.Theoretical, 20, course.Id);
        harness.Context.Set<Lecture>().Add(lecture);
        var activeEnrollment = new Enrollment(active.Id, course.Id, EnrollmentStatus.Active);
        var paidCanceledEnrollment = new Enrollment(paidCanceled.Id, course.Id, EnrollmentStatus.Active);
        paidCanceledEnrollment.ExtendPaidUntil(Now, 1);
        paidCanceledEnrollment.Transition(EnrollmentTrigger.Cancel, userId: 1, now: Now);
        var expiredCanceledEnrollment = new Enrollment(expiredCanceled.Id, course.Id, EnrollmentStatus.Active);
        expiredCanceledEnrollment.ExtendPaidUntil(Now.AddDays(-31), 30);
        expiredCanceledEnrollment.Transition(EnrollmentTrigger.Cancel, userId: 1, now: Now);
        harness.Context.Set<Enrollment>().AddRange(activeEnrollment, paidCanceledEnrollment, expiredCanceledEnrollment);
        await harness.Context.SaveChangesAsync();
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Equal([60, 61], call.EnrolledStudentUserIds.OrderBy(x => x));
    }

    [Fact]
    public async Task CancelAsync_NotifiesCanceledStudent_AtExactPaidUntil()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var student = new Student(63, Now);
        harness.Context.Set<Student>().Add(student);
        await harness.Context.SaveChangesAsync();
        var enrollment = new Enrollment(student.Id, harness.Course.Id, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now.AddDays(-30), 30);
        enrollment.Transition(EnrollmentTrigger.Cancel, userId: 1, now: Now);
        harness.Context.Set<Enrollment>().Add(enrollment);
        await harness.Context.SaveChangesAsync();
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Contains(63, call.EnrolledStudentUserIds);
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
    public async Task GetByIdForStudentAsync_ReturnsLecture_AfterCourseUnpublished_WhenEnrolledAndPaid()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Course>().Single(c => c.Id == harness.Course.Id).SetPublishedStatus(false);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var dto = await service.GetByIdForStudentAsync(harness.Lecture.Id);

        Assert.Equal(harness.Lecture.Id, dto.Id);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsCancelledLectureWithReason()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Instrument je pokvaren", 1, Now);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var dto = await service.GetByIdForStudentAsync(harness.Lecture.Id);

        Assert.True(dto.IsCancelled);
        Assert.Equal("Instrument je pokvaren", dto.CancellationReason);
        Assert.Equal(Now, dto.CancelledAt);
    }

    [Fact]
    public async Task GetPagedForStudentAsync_ReturnsCancelledLectures()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Instrument je pokvaren", 1, Now);
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetPagedForStudentAsync(new LectureSearchObject());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Instrument je pokvaren", dto.CancellationReason);
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
    public async Task CreateAsync_Throws_WhenLocationOverlaps_WithKeywordSpaces()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Overlap with spaces",
                Location = "  room 1  ",
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
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Razlog otkazivanja", 1, Now);
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
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Razlog otkazivanja", 1, Now);
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

    [Fact]
    public async Task GetByIdForInstructorAsync_CountsOnlyPresentAttendees()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var absent = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var pending = await AcademicTestData.AddEnrolledStudentAsync(harness, 61, Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        harness.Context.Set<Attendance>().Add(new Attendance(absent.Id, harness.Lecture.Id, AttendanceStatus.Absent));
        harness.Context.Set<Attendance>().Add(new Attendance(pending.Id, harness.Lecture.Id, AttendanceStatus.Pending));
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.GetByIdForInstructorAsync(harness.Lecture.Id);

        Assert.Equal(1, dto.AttendeeCount);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsAttendeeCount_AndMyAttendanceStatus()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var dto = await service.GetByIdForStudentAsync(harness.Lecture.Id);

        Assert.Equal(1, dto.AttendeeCount);
        Assert.Equal(AttendanceStatus.Present, dto.MyAttendanceStatus);
    }

    [Fact]
    public async Task GetPagedForInstructorAsync_ReturnsAttendeeCount()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor);

        var result = await service.GetPagedForInstructorAsync(new LectureSearchObject());

        var dto = Assert.Single(result.Items);
        Assert.Equal(1, dto.AttendeeCount);
    }

    [Fact]
    public async Task GetPagedForStudentAsync_ReturnsAttendeeCount_AndMyAttendanceStatus()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetPagedForStudentAsync(new LectureSearchObject());

        var dto = Assert.Single(result.Items);
        Assert.Equal(1, dto.AttendeeCount);
        Assert.Equal(AttendanceStatus.Present, dto.MyAttendanceStatus);
    }

    [Fact]
    public async Task CreateAsync_CreatesIndividualLecture_ForActivelyEnrolledStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(new LectureCreateRequest
        {
            CourseId = harness.Course.Id,
            Name = "Individual lesson",
            Location = "Room 2",
            Duration = 45,
            LectureTime = Now.AddDays(1),
            LectureType = LectureType.Practical,
            Capacity = 20,
            StudentId = harness.Student.Id
        });

        Assert.Equal(harness.Student.Id, dto.StudentId);
        Assert.True(dto.IsIndividual);
        Assert.Equal(1, dto.Capacity);
        Assert.Equal($"Student {harness.Student.Id}", dto.StudentName);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenStudentIsNotActivelyEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var stranger = new Student(77, Now);
        harness.Context.Set<Student>().Add(stranger);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Individual lesson",
                Location = "Room 2",
                Duration = 45,
                LectureTime = Now.AddDays(1),
                LectureType = LectureType.Practical,
                StudentId = stranger.Id
            }));

        Assert.Equal(Messages.LectureStudentNotActiveEnrollment, ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_IndividualLecture_KeepsCapacityOne()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var lecture = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(lecture.Id, new LectureUpdateRequest
        {
            Name = "Individual lesson",
            Location = "Room 2",
            Duration = 45,
            LectureTime = Now.AddDays(1),
            Capacity = 10
        });

        Assert.Equal(1, dto.Capacity);
    }

    [Fact]
    public async Task GetPagedForInstructorAsync_ReturnsStudentName_ForIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var service = CreateService(harness.Context, harness.Instructor);

        var result = await service.GetPagedForInstructorAsync(new LectureSearchObject { StudentId = harness.Student.Id });

        var dto = Assert.Single(result.Items);
        Assert.True(dto.IsIndividual);
        Assert.Equal($"Student {harness.Student.Id}", dto.StudentName);
    }

    [Fact]
    public async Task GetPagedForStudentAsync_HidesIndividualLecturesOfOtherStudents()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        await AcademicTestData.AddIndividualLectureAsync(harness, classmate, Now.AddDays(3));
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetPagedForStudentAsync(new LectureSearchObject());

        var dto = Assert.Single(result.Items);
        Assert.Equal(harness.Lecture.Id, dto.Id);
    }

    [Fact]
    public async Task GetPagedForStudentAsync_ReturnsOwnIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var own = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        await AcademicTestData.AddIndividualLectureAsync(harness, classmate, Now.AddDays(3));
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetPagedForStudentAsync(new LectureSearchObject());

        Assert.Equal(2, result.Items.Count);
        var individual = Assert.Single(result.Items, x => x.IsIndividual);
        Assert.Equal(own.Id, individual.Id);
    }

    [Fact]
    public async Task GetPagedForStudentAsync_WithFrom_OrdersUpcomingLecturesByTimeAscending()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var later = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(3));
        var sooner = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetPagedForStudentAsync(new LectureSearchObject { From = Now });

        Assert.Equal(new[] { harness.Lecture.Id, sooner.Id, later.Id }, result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetPagedForStudentAsync_WithoutFrom_KeepsDescendingOrder()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var later = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(3));
        var sooner = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetPagedForStudentAsync(new LectureSearchObject());

        Assert.Equal(new[] { later.Id, sooner.Id, harness.Lecture.Id }, result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_ForIndividualLectureOfAnotherStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var lecture = await AcademicTestData.AddIndividualLectureAsync(harness, classmate, Now.AddDays(3));
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(lecture.Id));
    }

    [Fact]
    public async Task CancelAsync_NotifiesOnlyAssignedStudent_ForIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var lecture = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Equal([harness.Student.AppUserId], call.EnrolledStudentUserIds);
    }

    [Fact]
    public async Task GetPagedForInstructorAsync_FiltersByLectureStatus()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var cancelled = new Lecture("Cancelled lesson", "Room 2", 60, Now.AddDays(1), LectureType.Theoretical, 30, harness.Course.Id);
        cancelled.Cancel("Instrument je pokvaren", harness.Instructor.AppUserId, Now);
        harness.Context.Set<Lecture>().Add(cancelled);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var scheduled = await service.GetPagedForInstructorAsync(new LectureSearchObject { LectureStatus = LectureStatus.Scheduled });
        var cancelledResult = await service.GetPagedForInstructorAsync(new LectureSearchObject { LectureStatus = LectureStatus.Cancelled });

        Assert.Equal(harness.Lecture.Id, Assert.Single(scheduled.Items).Id);
        Assert.Equal(cancelled.Id, Assert.Single(cancelledResult.Items).Id);
    }

    [Fact]
    public async Task GetByIdForInstructorAsync_Throws_ForOtherInstructorsLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForInstructorAsync(other.Lecture.Id));
    }

    [Fact]
    public async Task GetPagedForInstructorAsync_ReturnsOnlyOwnLectures()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var result = await service.GetPagedForInstructorAsync(new LectureSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal(harness.Lecture.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task CancelAsync_ExcludesStudentsOfOtherCourses_FromNotification()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, notificationDispatcher: dispatcher);

        await service.CancelAsync(harness.Lecture.Id, new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Equal([harness.Student.AppUserId], call.EnrolledStudentUserIds);
        Assert.DoesNotContain(other.Student.AppUserId, call.EnrolledStudentUserIds);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenStudentIsEnrolledOnlyInAnotherCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Individual lesson",
                Location = "Room 21",
                Duration = 45,
                LectureTime = Now.AddDays(2),
                LectureType = LectureType.Practical,
                StudentId = other.Student.Id
            }));

        Assert.Equal(Messages.LectureStudentNotActiveEnrollment, ex.Message);
    }

    [Fact]
    public async Task StudentLectureDtos_ReturnOwnAttendanceStatus_NotClassmates()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        harness.Context.Set<Attendance>().Add(new Attendance(classmate.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var byId = await service.GetByIdForStudentAsync(harness.Lecture.Id);
        var paged = await service.GetPagedForStudentAsync(new LectureSearchObject { Page = 1, PageSize = 10 });

        Assert.Null(byId.MyAttendanceStatus);
        Assert.Null(Assert.Single(paged.Items).MyAttendanceStatus);
    }

    [Fact]
    public async Task UpdateAsync_CountsOnlyPresentAttendancesAgainstCapacity()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        harness.Context.Set<Attendance>().AddRange(
            new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present),
            new Attendance(classmate.Id, harness.Lecture.Id, AttendanceStatus.Absent));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
        {
            Name = "First lesson",
            Location = "Room 1",
            Duration = 60,
            LectureTime = Now,
            Capacity = 1
        });

        Assert.Equal(1, dto.Capacity);
        Assert.Equal(1, dto.AttendeeCount);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Canceled)]
    [InlineData(EnrollmentStatus.Completed)]
    public async Task CreateAsync_Throws_WhenStudentEnrollmentIsNotActive(EnrollmentStatus status)
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var student = new Student(60, Now);
        harness.Context.Set<Student>().Add(student);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().Add(new Enrollment(student.Id, harness.Course.Id, status));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Individual lesson",
                Location = "Room 2",
                Duration = 45,
                LectureTime = Now.AddDays(1),
                LectureType = LectureType.Practical,
                StudentId = student.Id
            }));

        Assert.Equal(Messages.LectureStudentNotActiveEnrollment, ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenLocationOverlapsAnotherInstructorsLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
            {
                Name = "First lesson",
                Location = other.Lecture.Location,
                Duration = other.Lecture.Duration,
                LectureTime = other.Lecture.LectureTime
            }));
    }

    private static LectureService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null, ILectureNotificationDispatcher? notificationDispatcher = null)
    {
        var currentUser = actor ?? new StubCurrentActor();
        return new(context,
            currentUser,
            currentUser,
            AcademicTestData.CreateInstructorAccess(context, instructor),
            notificationDispatcher ?? new RecordingLectureNotificationDispatcher(),
            new StubDisplayNameService(),
            NullLogger<LectureService>.Instance,
            TestMapper.Create(),
            new FixedClock(Now));
    }
}
