using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data.Seed;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Data;

public sealed class DevelopmentDataSeedTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SeedEnrollments_SetsPaidUntil_ThirtyDaysFromUtcNow()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var clock = new FixedClock(Now);

        var student = new Student(0, Now);
        ctx.Set<Student>().Add(student);

        var instructor = new Instructor(1);
        ctx.Set<Instructor>().Add(instructor);
        await ctx.SaveChangesAsync();

        var course = new Course("Test Course", "Desc", 100m, Now, Now.AddMonths(1), instructor.Id);
        course.SetPublishedStatus(true);
        ctx.Set<Course>().Add(course);
        await ctx.SaveChangesAsync();

        await EnrollmentSeed.SeedEnrollments(ctx, clock);

        var enrollment = await ctx.Set<Enrollment>().SingleAsync();
        Assert.Equal(student.Id, enrollment.StudentId);
        Assert.Equal(course.Id, enrollment.CourseId);
        Assert.Equal(EnrollmentStatus.Active, enrollment.EnrollmentStatus);
        Assert.Equal(Now.AddDays(30), enrollment.PaidUntil);
    }

    [Fact]
    public async Task SeedCourses_CoursesAreRunningAtSeedTime()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var clock = new FixedClock(Now);

        ctx.Set<Instructor>().Add(new Instructor(1));
        await ctx.SaveChangesAsync();

        await CourseSeed.SeedCourses(ctx, clock);

        var courses = await ctx.Set<Course>().ToListAsync();
        Assert.Equal(2, courses.Count);
        Assert.All(courses, c =>
        {
            Assert.True(c.StartDate < Now);
            Assert.True(c.EndDate > Now);
        });
    }
}
