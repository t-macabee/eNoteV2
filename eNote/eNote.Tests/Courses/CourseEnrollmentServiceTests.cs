using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eNote.Tests.Courses;

public sealed class CourseEnrollmentServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EnrollAsync_CreatesActiveEnrollment_ForPublishedCourse()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.CreatedById);
    }

    [Fact]
    public async Task EnrollAsync_ReactivatesCanceledEnrollment()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Canceled));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
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
    }

    [Fact]
    public async Task UnenrollAsync_Succeeds_WhenMembershipIsInactive()
    {
        // Unenrollment does not check membership — you can always leave a course.
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

    private static ENoteContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(storeId: 1));
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

    private static CourseEnrollmentService CreateService(ENoteContext context, Student student) =>
        new(
            context,
            new FixedClock(Now),
            new StubCurrentActor(student: student),
            NullLogger<CourseEnrollmentService>.Instance);
}
