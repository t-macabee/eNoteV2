using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
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
    public async Task SeedEnrollments_EnrollsEveryStudent()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var clock = new FixedClock(Now);

        ctx.Set<Student>().AddRange(new Student(1, Now), new Student(2, Now));
        ctx.Set<Instructor>().Add(new Instructor(3));
        await ctx.SaveChangesAsync();

        var course = new Course("Test Course", "Desc", 100m, Now, Now.AddMonths(1), 1);
        course.SetPublishedStatus(true);
        ctx.Set<Course>().Add(course);
        await ctx.SaveChangesAsync();

        await EnrollmentSeed.SeedEnrollments(ctx, clock);

        var enrollments = await ctx.Set<Enrollment>().ToListAsync();
        Assert.Equal(2, enrollments.Count);
        Assert.All(enrollments, e => Assert.Equal(course.Id, e.CourseId));
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

    [Fact]
    public async Task SeedAsync_OnEmptyDb_CreatesDataForEveryFeature()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        await ctx.Database.EnsureCreatedAsync();
        await using var provider = SeedTestHarness.BuildProvider(ctx, Now);
        var clock = new FixedClock(Now);

        await IdentitySeed.SeedAsync(provider);
        await DevelopmentDataSeed.SeedAsync(ctx, clock);

        var users = await ctx.Users.ToListAsync();
        Assert.Equal(12, users.Count);
        Assert.All(users, u =>
        {
            Assert.False(string.IsNullOrWhiteSpace(u.FirstName));
            Assert.False(string.IsNullOrWhiteSpace(u.LastName));
        });

        Assert.Equal(8, await ctx.Set<Student>().CountAsync());
        Assert.Equal(16, await ctx.Set<Enrollment>().CountAsync());

        var lectures = await ctx.Set<Lecture>().ToListAsync();
        Assert.Equal(9, lectures.Count);
        Assert.Equal(5, lectures.Count(l => l.LectureStatus == LectureStatus.Held));

        Assert.Equal(48, await ctx.Set<Attendance>().CountAsync());
        Assert.Equal(6, await ctx.Set<Assignment>().CountAsync());

        var submissions = await ctx.Set<AssignmentSubmission>().ToListAsync();
        Assert.Equal(40, submissions.Count);
        Assert.All(submissions, s =>
        {
            Assert.NotNull(s.SubmittedAt);
            Assert.NotNull(s.Grade);
        });

        Assert.Equal(5, await ctx.Set<LectureNote>().CountAsync());
        Assert.Equal(6, await ctx.Set<Announcement>().CountAsync());

        var events = await ctx.Set<Event>().ToListAsync();
        Assert.Equal(3, events.Count);
        Assert.Single(events, e => e.CourseId != null);

        var rentals = await ctx.Set<InstrumentRental>().IgnoreQueryFilters().ToListAsync();
        Assert.Equal(23, rentals.Count);
        Assert.Equal(3, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.Pending));
        Assert.Equal(2, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.Approved));
        Assert.Equal(3, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.Active));
        Assert.Equal(9, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.Completed));
        Assert.Equal(2, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.ReturnedEarly));
        Assert.Equal(2, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.Rejected));
        Assert.Equal(2, rentals.Count(r => r.RentalStatus == InstrumentRentalStatus.Canceled));
        Assert.Equal(10, rentals.Count(r => r.IsPaid));

        Assert.Equal(4, await ctx.Set<InstrumentView>().CountAsync());

        var notifications = await ctx.Set<Notification>().ToListAsync();
        Assert.Equal(3, notifications.Count);
        Assert.Single(notifications, n => n.IsRead);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_AddsNothing()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        await ctx.Database.EnsureCreatedAsync();
        await using var provider = SeedTestHarness.BuildProvider(ctx, Now);
        var clock = new FixedClock(Now);

        await IdentitySeed.SeedAsync(provider);
        await DevelopmentDataSeed.SeedAsync(ctx, clock);

        await IdentitySeed.SeedAsync(provider);
        await DevelopmentDataSeed.SeedAsync(ctx, clock);

        Assert.Equal(12, await ctx.Users.CountAsync());
        Assert.Equal(8, await ctx.Set<Student>().CountAsync());
        Assert.Equal(16, await ctx.Set<Enrollment>().CountAsync());
        Assert.Equal(9, await ctx.Set<Lecture>().CountAsync());
        Assert.Equal(48, await ctx.Set<Attendance>().CountAsync());
        Assert.Equal(6, await ctx.Set<Assignment>().CountAsync());
        Assert.Equal(40, await ctx.Set<AssignmentSubmission>().CountAsync());
        Assert.Equal(5, await ctx.Set<LectureNote>().CountAsync());
        Assert.Equal(6, await ctx.Set<Announcement>().CountAsync());
        Assert.Equal(3, await ctx.Set<Event>().CountAsync());
        Assert.Equal(23, await ctx.Set<InstrumentRental>().IgnoreQueryFilters().CountAsync());
        Assert.Equal(10, await ctx.Set<InstrumentRental>().IgnoreQueryFilters().CountAsync(r => r.IsPaid));
        Assert.Equal(4, await ctx.Set<InstrumentView>().CountAsync());
        Assert.Equal(3, await ctx.Set<Notification>().CountAsync());
    }

    [Fact]
    public async Task SeedAsync_RespectsUniqueIndexes()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        await ctx.Database.EnsureCreatedAsync();
        await using var provider = SeedTestHarness.BuildProvider(ctx, Now);
        var clock = new FixedClock(Now);

        await IdentitySeed.SeedAsync(provider);
        await DevelopmentDataSeed.SeedAsync(ctx, clock);

        var rentals = await ctx.Set<InstrumentRental>().IgnoreQueryFilters().ToListAsync();

        var approvedOrActive = rentals.Where(r => r.RentalStatus.BlocksInstrument()).ToList();
        var duplicateInstrumentLocks = approvedOrActive
            .GroupBy(r => r.InstrumentId)
            .Where(g => g.Count() > 1)
            .ToList();
        Assert.Empty(duplicateInstrumentLocks);

        var pendingRentals = rentals.Where(r => r.RentalStatus == InstrumentRentalStatus.Pending).ToList();
        var duplicatePending = pendingRentals
            .GroupBy(r => new { r.InstrumentId, r.StudentProfileId })
            .Where(g => g.Count() > 1)
            .ToList();
        Assert.Empty(duplicatePending);

        var enrollments = await ctx.Set<Enrollment>().ToListAsync();
        var duplicateEnrollments = enrollments
            .GroupBy(e => new { e.StudentId, e.CourseId })
            .Where(g => g.Count() > 1)
            .ToList();
        Assert.Empty(duplicateEnrollments);

        var attendances = await ctx.Set<Attendance>().ToListAsync();
        var duplicateAttendances = attendances
            .GroupBy(a => new { a.StudentId, a.LectureId })
            .Where(g => g.Count() > 1)
            .ToList();
        Assert.Empty(duplicateAttendances);

        var submissions = await ctx.Set<AssignmentSubmission>().ToListAsync();
        var duplicateSubmissions = submissions
            .GroupBy(s => new { s.AssignmentId, s.StudentId })
            .Where(g => g.Count() > 1)
            .ToList();
        Assert.Empty(duplicateSubmissions);

        var views = await ctx.Set<InstrumentView>().ToListAsync();
        var duplicateViews = views
            .GroupBy(v => new { v.UserId, v.InstrumentId })
            .Where(g => g.Count() > 1)
            .ToList();
        Assert.Empty(duplicateViews);
    }

    [Fact]
    public async Task SeedAsync_MobileHasUnpaidDebtAndStudentHasNone()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        await ctx.Database.EnsureCreatedAsync();
        await using var provider = SeedTestHarness.BuildProvider(ctx, Now);
        var clock = new FixedClock(Now);

        await IdentitySeed.SeedAsync(provider);
        await DevelopmentDataSeed.SeedAsync(ctx, clock);

        var mobileUser = await ctx.Users.SingleAsync(u => u.UserName == "mobile");
        var studentUser = await ctx.Users.SingleAsync(u => u.UserName == "student");

        var rentals = await ctx.Set<InstrumentRental>().IgnoreQueryFilters().Include(r => r.StudentProfile).ToListAsync();

        var mobileUnpaidDebt = rentals.Where(r => r.StudentProfile.AppUserId == mobileUser.Id &&
            (r.RentalStatus == InstrumentRentalStatus.Completed || r.RentalStatus == InstrumentRentalStatus.ReturnedEarly) &&
            !r.IsPaid).ToList();
        Assert.Single(mobileUnpaidDebt);

        var studentUnpaidDebt = rentals.Where(r => r.StudentProfile.AppUserId == studentUser.Id &&
            (r.RentalStatus == InstrumentRentalStatus.Completed || r.RentalStatus == InstrumentRentalStatus.ReturnedEarly) &&
            !r.IsPaid).ToList();
        Assert.Empty(studentUnpaidDebt);
    }
}
