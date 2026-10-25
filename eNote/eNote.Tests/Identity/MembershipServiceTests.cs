using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Identity;

public sealed class MembershipServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RecordPaymentAsync_SetsMembershipPaidUntil_AndStartsThePeriodToday()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var notifications = new RecordingMembershipWarningDispatcher();
        var service = CreateService(context, notifications, instructor: null);

        var payment = await service.RecordPaymentAsync(null, student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 50m,
            ReceiptNumber = "CL-1",
            Note = "Gotovina",
            Months = 1
        });

        var stored = await context.Set<Student>().SingleAsync(s => s.Id == student.Id);
        Assert.Equal(Now.Date, payment.PeriodStart);
        Assert.Equal(Now.Date.AddMonths(1), payment.PeriodEnd);
        Assert.Equal(Now.Date, payment.PaidAt);
        Assert.Equal("CL-1", payment.ReceiptNumber);
        Assert.Equal("Gotovina", payment.Note);
        Assert.Equal(Now.Date.AddMonths(1), stored.MembershipPaidUntil);
        Assert.Equal(1, await context.Set<MembershipPayment>().CountAsync());
    }

    [Fact]
    public async Task RecordPaymentAsync_ContinuesFromTheCurrentPaidUntil_WhenItIsInTheFuture()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        student.UpdateMembership(Now.Date.AddDays(10));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);

        var payment = await service.RecordPaymentAsync(null, student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 50m,
            ReceiptNumber = "CL-2",
            PeriodEnd = Now.Date.AddMonths(2)
        });

        Assert.Equal(Now.Date.AddDays(10), payment.PeriodStart);
        Assert.Equal(Now.Date.AddMonths(2), payment.PeriodEnd);
    }

    [Fact]
    public async Task RecordPaymentAsync_Throws_WhenPeriodEndIsNotAfterPeriodStart()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.RecordPaymentAsync(null, student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 50m,
            ReceiptNumber = "CL-3",
            PeriodEnd = Now.Date
        }));

        Assert.Equal(Messages.MembershipPeriodEndBeforeStart, exception.Message);
        Assert.Empty(context.Set<MembershipPayment>());
    }

    [Fact]
    public async Task RecordPaymentAsync_Throws_WhenTheReceiptNumberIsAlreadyRecorded()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);
        var exception = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.MembershipPaymentReceiptNumberUniqueIndex}\""));
        var throwing = CreateService(new ThrowingSaveDbContext(context, exception), new RecordingMembershipWarningDispatcher(), instructor: null);

        var thrown = await Assert.ThrowsAsync<BusinessException>(() => throwing.RecordPaymentAsync(null, student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 50m,
            ReceiptNumber = "CL-4",
            Months = 1
        }));

        Assert.Equal(Messages.MembershipReceiptTaken, thrown.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_ThrowsNotFound_WhenInstructorCannotSeeTheStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RecordPaymentAsync(instructor.Id, student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 50m,
            ReceiptNumber = "CL-5",
            Months = 1
        }));
    }

    [Fact]
    public async Task RecordPaymentAsync_Succeeds_WhenInstructorTeachesTheStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now, currentUser: new StubCurrentActor(userId: 100));
        var (instructor, student) = await SeedInstructorAndEnrolledStudentAsync(context);
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor);

        var payment = await service.RecordPaymentAsync(instructor.Id, student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 50m,
            ReceiptNumber = "CL-6",
            Months = 2
        });

        Assert.Equal(instructor.AppUserId, (await context.Set<MembershipPayment>().SingleAsync(p => p.Id == payment.Id)).CreatedById);
        Assert.Equal(Now.Date.AddMonths(2), payment.PeriodEnd);
    }

    [Fact]
    public async Task GetPaymentsAsync_ReturnsNewestFirst_AndPages()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        context.Set<MembershipPayment>().AddRange(
            new MembershipPayment(student.Id, 50m, Now.Date.AddMonths(-2), Now.Date.AddMonths(-2), Now.Date.AddMonths(-1), "CL-A", null),
            new MembershipPayment(student.Id, 50m, Now.Date.AddMonths(-1), Now.Date.AddMonths(-1), Now.Date, "CL-B", null),
            new MembershipPayment(student.Id, 50m, Now.Date, Now.Date, Now.Date.AddMonths(1), "CL-C", null));
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);

        var firstPage = await service.GetPaymentsAsync(null, student.Id, new MembershipPaymentSearchObject { Page = 1, PageSize = 2 });

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(["CL-C", "CL-B"], firstPage.Items.Select(p => p.ReceiptNumber));
    }

    [Fact]
    public async Task GetPaymentsAsync_ThrowsNotFound_WhenInstructorCannotSeeTheStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPaymentsAsync(instructor.Id, student.Id, new MembershipPaymentSearchObject()));
    }

    [Fact]
    public async Task SendWarningAsync_RecordsTheSendTime_AndDispatchesTheWarning()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        student.UpdateMembership(Now.Date.AddDays(5));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var notifications = new RecordingMembershipWarningDispatcher();
        var service = CreateService(context, notifications, instructor: null);

        await service.SendWarningAsync(null, student.Id, new SendMembershipWarningRequest { Message = "Obnovite članarinu." });

        var stored = await context.Set<Student>().SingleAsync(s => s.Id == student.Id);
        Assert.Equal(Now, stored.MembershipWarningSentAt);
        var warning = Assert.Single(notifications.Warnings);
        Assert.Equal(student.AppUserId, warning.StudentUserId);
        Assert.Equal(Now.Date.AddDays(5), warning.MembershipPaidUntil);
        Assert.Equal("Obnovite članarinu.", warning.Message);
    }

    [Fact]
    public async Task SendWarningAsync_Throws_WhenMembershipLastsLongerThanTheWarningWindow()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        student.UpdateMembership(Now.Date.AddDays(8));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SendWarningAsync(null, student.Id, new SendMembershipWarningRequest()));

        Assert.Equal(Messages.MembershipWarningNotAllowed, exception.Message);
    }

    [Fact]
    public async Task SendWarningAsync_Throws_WhenAWarningWasSentWithinTwentyFourHours()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        student.UpdateMembership(Now.Date.AddDays(-1));
        student.RecordMembershipWarning(Now.AddHours(-2));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SendWarningAsync(null, student.Id, new SendMembershipWarningRequest()));

        Assert.Equal(Messages.MembershipWarningRecentlySent, exception.Message);
    }

    [Fact]
    public async Task SendWarningAsync_Succeeds_WhenExpiredMembershipWarningIsOlderThanTwentyFourHours()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        student.UpdateMembership(Now.Date.AddDays(-2));
        student.RecordMembershipWarning(Now.AddHours(-25));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingMembershipWarningDispatcher(), instructor: null);

        await service.SendWarningAsync(null, student.Id, new SendMembershipWarningRequest());

        var stored = await context.Set<Student>().SingleAsync(s => s.Id == student.Id);
        Assert.Equal(Now, stored.MembershipWarningSentAt);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void IsWarningDue_AllowsOnlyExpiredOrNearlyExpiredMembership(int? daysFromToday, bool expected)
    {
        var paidUntil = daysFromToday.HasValue ? Now.Date.AddDays(daysFromToday.Value) : (DateTime?)null;

        Assert.Equal(expected, MembershipService.IsWarningDue(paidUntil, Now));
    }

    private static MembershipService CreateService(
        IAppDbContext context,
        RecordingMembershipWarningDispatcher notifications,
        Instructor? instructor)
    {
        var instructorAccess = new InstructorAccessService(
            context,
            new StubUserProfileLookup(instructor: instructor));

        return new MembershipService(
            context,
            new AdminStudentService(context, new StubUserIdentityService(), instructorAccess),
            new FixedClock(Now),
            notifications);
    }

    private static async Task<(Instructor Instructor, Student Student)> SeedInstructorAndEnrolledStudentAsync(ENoteContext context)
    {
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Gitara", null, 100m, Now, Now.AddMonths(3), instructor.Id) { CreatedById = instructor.AppUserId };
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();

        return (instructor, student);
    }
}
