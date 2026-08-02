using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Academic;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class LectureAttendanceServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RsvpAsync_Confirm_CreatesPresentAttendance()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var response = await service.RsvpAsync(harness.Lecture.Id, new RsvpRequest { Confirm = true });

        Assert.True(response.Confirmed);
        var attendance = await harness.Context.Set<Attendance>().SingleAsync();
        Assert.Equal(AttendanceStatus.Present, attendance.AttendanceStatus);
    }

    [Fact]
    public async Task RsvpAsync_Throws_WhenLectureFull()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).UpdateDetails(
            "First lesson", "Room 1", 60, Now, capacity: 1);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var otherStudent = new Student(88, Now);
        harness.Context.Set<Student>().Add(otherStudent);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().Add(new Enrollment(otherStudent.Id, harness.Course.Id, EnrollmentStatus.Active));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, otherStudent);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RsvpAsync(harness.Lecture.Id, new RsvpRequest { Confirm = true }));
    }

    [Fact]
    public async Task RsvpAsync_Decline_MarksExistingAttendanceAbsent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var response = await service.RsvpAsync(harness.Lecture.Id, new RsvpRequest { Confirm = false });

        Assert.False(response.Confirmed);
        var attendance = await harness.Context.Set<Attendance>().SingleAsync();
        Assert.Equal(AttendanceStatus.Absent, attendance.AttendanceStatus);
    }

    [Fact]
    public async Task RsvpAsync_Throws_WhenStudentNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var stranger = new Student(77, Now);
        harness.Context.Set<Student>().Add(stranger);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, stranger);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.RsvpAsync(harness.Lecture.Id, new RsvpRequest { Confirm = true }));
    }

    [Fact]
    public async Task MarkAttendanceAsync_CreatesAttendance_ForEnrolledStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var dto = await service.MarkAttendanceAsync(harness.Lecture.Id, new MarkAttendanceRequest
        {
            StudentId = harness.Student.Id,
            AttendanceStatus = AttendanceStatus.Present
        });

        Assert.Equal(AttendanceStatus.Present, dto.AttendanceStatus);
        Assert.Equal(harness.Student.Id, dto.StudentId);
    }

    [Fact]
    public async Task MarkAttendanceAsync_Throws_WhenStudentNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var stranger = new Student(77, Now);
        harness.Context.Set<Student>().Add(stranger);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.MarkAttendanceAsync(harness.Lecture.Id, new MarkAttendanceRequest
            {
                StudentId = stranger.Id,
                AttendanceStatus = AttendanceStatus.Present
            }));
    }

    [Fact]
    public async Task GetAttendanceAsync_Throws_WhenLectureNotOwned()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor, harness.Student);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.GetAttendanceAsync(harness.Lecture.Id, new AttendanceSearchObject { Page = 1, PageSize = 10 }));
    }

    private static LectureAttendanceService CreateService(ENoteContext context, Instructor instructor, Student student) =>
        new(context,
            new StubCurrentActor(student: student),
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            NullLogger<LectureAttendanceService>.Instance);

    private sealed class StubDisplayNameService : IStudentDisplayNameService
    {
        public Task<string> GetStudentDisplayNameAsync(Student student) => Task.FromResult($"Student {student.Id}");
        public Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(students.ToDictionary(s => s.Id, s => $"Student {s.Id}"));
    }
}
