using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.LectureNotes.Services;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class AcademicGatingTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AssignmentService_GatedForUnpaid_OpenForPaid_AndFreeCourse()
    {
        var (context, instructor, paidStudent, unpaidStudent, course, lecture) = await SetupScenarioAsync();
        var assignment = new Assignment("Homework", "Do exercises", Now.AddDays(7), lecture.Id);
        context.Set<Assignment>().Add(assignment);
        await context.SaveChangesAsync();

        var paidActor = new StubCurrentActor(student: paidStudent);
        var unpaidActor = new StubCurrentActor(student: unpaidStudent);

        var paidService = AcademicTestData.CreateService<AssignmentService>(context, instructor, paidActor, Now);
        var unpaidService = AcademicTestData.CreateService<AssignmentService>(context, instructor, unpaidActor, Now);

        var paidList = await paidService.GetForStudentAsync(new AssignmentSearchObject());
        Assert.Single(paidList.Items);
        var paidItem = await paidService.GetByIdForStudentAsync(assignment.Id);
        Assert.Equal("Homework", paidItem.Title);

        var unpaidList = await unpaidService.GetForStudentAsync(new AssignmentSearchObject());
        Assert.Empty(unpaidList.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => unpaidService.GetByIdForStudentAsync(assignment.Id));

        course.UpdateDetails(course.Name, course.Description, 0m, course.StartDate, course.EndDate);
        await context.SaveChangesAsync();

        var freeList = await unpaidService.GetForStudentAsync(new AssignmentSearchObject());
        Assert.Single(freeList.Items);
        var freeItem = await unpaidService.GetByIdForStudentAsync(assignment.Id);
        Assert.Equal("Homework", freeItem.Title);
    }

    [Fact]
    public async Task LectureNoteService_GatedForUnpaid_OpenForPaid()
    {
        var (context, instructor, paidStudent, unpaidStudent, _, lecture) = await SetupScenarioAsync();
        var note = new LectureNote("Chords", "C major", lecture.Id);
        context.Set<LectureNote>().Add(note);
        await context.SaveChangesAsync();

        var paidService = AcademicTestData.CreateService<LectureNoteService>(context, instructor, new StubCurrentActor(student: paidStudent), Now);
        var unpaidService = AcademicTestData.CreateService<LectureNoteService>(context, instructor, new StubCurrentActor(student: unpaidStudent), Now);

        var paidNotes = await paidService.GetForStudentAsync(lecture.Id, new LectureNoteSearchObject());
        Assert.Single(paidNotes.Items);
        var paidNote = await paidService.GetByIdForStudentAsync(lecture.Id, note.Id);
        Assert.Equal("Chords", paidNote.Title);

        var unpaidNotes = await unpaidService.GetForStudentAsync(lecture.Id, new LectureNoteSearchObject());
        Assert.Empty(unpaidNotes.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => unpaidService.GetByIdForStudentAsync(lecture.Id, note.Id));
    }

    [Fact]
    public async Task LectureService_GatedForUnpaid_OpenForPaid()
    {
        var (context, instructor, paidStudent, unpaidStudent, _, lecture) = await SetupScenarioAsync();

        var paidService = CreateLectureService(context, instructor, new StubCurrentActor(student: paidStudent));
        var unpaidService = CreateLectureService(context, instructor, new StubCurrentActor(student: unpaidStudent));

        var paidLectures = await paidService.GetPagedForStudentAsync(new LectureSearchObject());
        Assert.Single(paidLectures.Items);
        var paidLecture = await paidService.GetByIdForStudentAsync(lecture.Id);
        Assert.Equal(lecture.Id, paidLecture.Id);

        var unpaidLectures = await unpaidService.GetPagedForStudentAsync(new LectureSearchObject());
        Assert.Empty(unpaidLectures.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => unpaidService.GetByIdForStudentAsync(lecture.Id));
    }

    [Fact]
    public async Task RankingService_GatedForUnpaid_OpenForPaid()
    {
        var (context, instructor, paidStudent, unpaidStudent, course, lecture) = await SetupScenarioAsync();
        var assignment = new Assignment("HW", "Desc", Now.AddDays(7), lecture.Id);
        context.Set<Assignment>().Add(assignment);
        var submission = new AssignmentSubmission(assignment.Id, paidStudent.Id);
        submission.Submit("/hw.pdf", Now);
        submission.SetGrade(95);
        context.Set<AssignmentSubmission>().Add(submission);
        await context.SaveChangesAsync();

        var paidService = CreateRankingService(context, instructor, paidStudent);
        var unpaidService = CreateRankingService(context, instructor, unpaidStudent);

        var ranking = await paidService.GetForStudentAsync(course.Id);
        Assert.Single(ranking);

        await Assert.ThrowsAsync<AuthorizationException>(() => unpaidService.GetForStudentAsync(course.Id));
    }

    [Fact]
    public async Task LectureAttendanceService_RsvpGated_InstructorMarkAttendanceNotGated()
    {
        var (context, instructor, paidStudent, unpaidStudent, _, lecture) = await SetupScenarioAsync();

        var paidService = CreateAttendanceService(context, instructor, paidStudent);
        var unpaidService = CreateAttendanceService(context, instructor, unpaidStudent);

        var paidRsvp = await paidService.RsvpAsync(lecture.Id, new RsvpRequest { Confirm = true });
        Assert.True(paidRsvp.Confirmed);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            unpaidService.RsvpAsync(lecture.Id, new RsvpRequest { Confirm = true }));
        Assert.Equal(Messages.StudentNotEnrolled, ex.Message);

        var instructorService = CreateAttendanceService(context, instructor, paidStudent, new StubCurrentActor(instructor: instructor));
        var marked = await instructorService.MarkAttendanceAsync(lecture.Id, new MarkAttendanceRequest
        {
            StudentId = unpaidStudent.Id,
            AttendanceStatus = AttendanceStatus.Present
        });
        Assert.Equal(AttendanceStatus.Present, marked.AttendanceStatus);
    }

    [Fact]
    public async Task AssignmentSubmissionService_OwnSubmissionAndHistory_Gated()
    {
        var (context, instructor, paidStudent, unpaidStudent, _, lecture) = await SetupScenarioAsync();
        var assignment = new Assignment("HW", "Desc", Now.AddDays(7), lecture.Id);
        context.Set<Assignment>().Add(assignment);
        var submission = new AssignmentSubmission(assignment.Id, unpaidStudent.Id);
        submission.Submit("/hw.pdf", Now);
        submission.SetGrade(80);
        context.Set<AssignmentSubmission>().Add(submission);
        await context.SaveChangesAsync();

        var unpaidActor = new StubCurrentActor(student: unpaidStudent);
        var service = new AssignmentSubmissionService(
            context,
            new FixedClock(Now),
            unpaidActor,
            unpaidActor,
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            new RecordingFileStorageService(),
            new NoOpSubmissionNotificationDispatcher(),
            TestMapper.Create());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetOwnSubmissionAsync(assignment.Id));
        var history = await service.GetHistoryForStudentAsync(new AssignmentSubmissionSearchObject());
        Assert.Empty(history.Items);

        var enrollment = await context.Set<Enrollment>().SingleAsync(e => e.StudentId == unpaidStudent.Id && e.CourseId == lecture.CourseId);
        enrollment.ExtendPaidUntil(Now, 30);
        await context.SaveChangesAsync();

        var own = await service.GetOwnSubmissionAsync(assignment.Id);
        Assert.Equal(80, own.Grade);
        history = await service.GetHistoryForStudentAsync(new AssignmentSubmissionSearchObject());
        Assert.Single(history.Items);
    }

    [Fact]
    public async Task LectureService_CancelNotifiesUnpaidStudent()
    {
        var (context, instructor, _, unpaidStudent, _, lecture) = await SetupScenarioAsync();
        var dispatcher = new RecordingLectureNotificationDispatcher();
        var instructorService = CreateLectureService(context, instructor, new StubCurrentActor(instructor: instructor), dispatcher);

        await instructorService.CancelAsync(lecture.Id);

        var call = Assert.Single(dispatcher.CancelledCalls);
        Assert.Contains(unpaidStudent.AppUserId, call.EnrolledStudentUserIds);
    }

    private static async Task<(ENoteContext Context, Instructor Instructor, Student PaidStudent, Student UnpaidStudent, Course Course, Lecture Lecture)> SetupScenarioAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);

        var unpaidStudent = new Student(99, Now);
        context.Set<Student>().Add(unpaidStudent);
        await context.SaveChangesAsync();

        var unpaidEnrollment = new Enrollment(unpaidStudent.Id, harness.Course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(unpaidEnrollment);
        await context.SaveChangesAsync();

        return (context, harness.Instructor, harness.Student, unpaidStudent, harness.Course, harness.Lecture);
    }

    private static LectureService CreateLectureService(ENoteContext context, Instructor instructor, StubCurrentActor actor, ILectureNotificationDispatcher? dispatcher = null) =>
        new(context,
            actor,
            actor,
            AcademicTestData.CreateInstructorAccess(context, instructor),
            dispatcher ?? new NoOpLectureNotificationDispatcher(),
            NullLogger<LectureService>.Instance,
            TestMapper.Create(),
            new FixedClock(Now));

    private static RankingService CreateRankingService(ENoteContext context, Instructor instructor, Student student)
    {
        var actor = new StubCurrentActor(student: student);
        return new(context,
            actor,
            actor,
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            new FixedClock(Now));
    }

    private static LectureAttendanceService CreateAttendanceService(ENoteContext context, Instructor instructor, Student student, StubCurrentActor? actor = null)
    {
        var current = actor ?? new StubCurrentActor(student: student);
        return new(context,
            current,
            current,
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            NullLogger<LectureAttendanceService>.Instance,
            new FixedClock(Now));
    }
}
