using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using eNote.Domain.Entities.Communication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Identity;

public sealed class UserSelfServiceDeleteProfileTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeleteProfileAsync_Throws_WhenCurrentPasswordIsWrong()
    {
        var harness = await CreateHarnessAsync();
        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<CurrentPasswordInvalidException>(() =>
            service.DeleteProfileAsync(new DeleteProfileRequest { CurrentPassword = "Wrong1!" }));

        Assert.Equal(Messages.CurrentPasswordIncorrect, exception.Message);
        Assert.Equal("error.current_password_invalid", exception.ErrorCode);
        var user = await harness.UserManager.FindByIdAsync(harness.Student.AppUserId.ToString());
        Assert.True(user!.IsActive);
        Assert.Equal("student", user.UserName);
    }

    [Fact]
    public async Task DeleteProfileAsync_Blocks_WhenACompletedRentalIsUnpaid()
    {
        var harness = await CreateHarnessAsync();
        var rental = await SeedRentalAsync(harness, completed: true, paid: false);
        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => DeleteAsync(service));

        Assert.Equal(Messages.ProfileDeleteBlockedByUnpaidRental, exception.Message);
        var stored = await harness.Context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(InstrumentRentalStatus.Completed, stored.RentalStatus);
        var user = await harness.UserManager.FindByIdAsync(harness.Student.AppUserId.ToString());
        Assert.True(user!.IsActive);
    }

    [Fact]
    public async Task DeleteProfileAsync_Blocks_WhenARentalIsApprovedOrActive()
    {
        var harness = await CreateHarnessAsync();
        var rental = await SeedRentalAsync(harness, completed: false, paid: false);
        rental.Approve(50m, null, Now.AddDays(-1), 1);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => DeleteAsync(service));

        Assert.Equal(Messages.ProfileDeleteBlockedByOpenRental, exception.Message);
    }

    [Fact]
    public async Task DeleteProfileAsync_ListsEveryBlockingRentalReason()
    {
        var harness = await CreateHarnessAsync();
        await SeedRentalAsync(harness, completed: true, paid: false);
        var approved = await SeedRentalAsync(harness, completed: false, paid: false);
        approved.Approve(50m, null, Now.AddDays(-1), 1);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => DeleteAsync(service));

        Assert.Equal(
            $"{Messages.ProfileDeleteBlockedByUnpaidRental} {Messages.ProfileDeleteBlockedByOpenRental}",
            exception.Message);
    }

    [Fact]
    public async Task DeleteProfileAsync_Blocks_WhenATuitionPaymentIsProcessing()
    {
        var harness = await CreateHarnessAsync();
        var enrollment = await SeedActiveEnrollmentAsync(harness);
        harness.Context.Set<CoursePayment>().Add(new CoursePayment(
            enrollment.Id,
            "pi_processing",
            10000,
            "BAM",
            PaymentStatus.RequiresAction));
        await harness.Context.SaveChangesAsync();
        harness.Gateway.AddIntent("pi_processing", 10000, "BAM", status: "processing");
        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => DeleteAsync(service));

        Assert.Equal(Messages.PaymentProcessing, exception.Message);
        Assert.Empty(harness.Gateway.CancelCalls);
        var stored = await harness.Context.Set<Enrollment>().SingleAsync(x => x.Id == enrollment.Id);
        Assert.Equal(EnrollmentStatus.Active, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task DeleteProfileAsync_BlockedByUnpaidRental_CancelsNoPaymentIntent()
    {
        var harness = await CreateHarnessAsync();
        var rental = await SeedRentalAsync(harness, completed: true, paid: false);
        harness.Context.Set<RentalPayment>().Add(new RentalPayment(
            rental.Id,
            rental.MusicStoreId,
            "pi_unpaid",
            5000,
            "BAM",
            PaymentStatus.RequiresAction));
        await harness.Context.SaveChangesAsync();
        harness.Gateway.AddIntent("pi_unpaid", 5000, "BAM");
        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => DeleteAsync(service));

        Assert.Equal(Messages.ProfileDeleteBlockedByUnpaidRental, exception.Message);
        Assert.Empty(harness.Gateway.RetrieveCalls);
        Assert.Empty(harness.Gateway.CancelCalls);
        var payment = await harness.Context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.RequiresAction, payment.Status);
    }

    [Fact]
    public async Task DeleteProfileAsync_CancelsOpenPaymentIntentsForRentalsAndTuition()
    {
        var harness = await CreateHarnessAsync();
        var enrollment = await SeedActiveEnrollmentAsync(harness);
        var rental = await SeedRentalAsync(harness, completed: true, paid: true);
        harness.Context.Set<CoursePayment>().Add(new CoursePayment(
            enrollment.Id,
            "pi_tuition",
            10000,
            "BAM",
            PaymentStatus.RequiresAction));
        harness.Context.Set<RentalPayment>().Add(new RentalPayment(
            rental.Id,
            rental.MusicStoreId,
            "pi_rental",
            5000,
            "BAM",
            PaymentStatus.Failed));
        await harness.Context.SaveChangesAsync();
        harness.Gateway.AddIntent("pi_tuition", 10000, "BAM");
        harness.Gateway.AddIntent("pi_rental", 5000, "BAM");
        var service = CreateService(harness);

        await DeleteAsync(service);

        Assert.Equal(
            ["pi_rental", "pi_tuition"],
            harness.Gateway.CancelCalls.Select(x => x.PaymentIntentId));
        Assert.All(
            await harness.Context.Set<CoursePayment>().ToListAsync(),
            payment => Assert.Equal(PaymentStatus.Canceled, payment.Status));
        Assert.All(
            await harness.Context.Set<RentalPayment>().ToListAsync(),
            payment => Assert.Equal(PaymentStatus.Canceled, payment.Status));
    }

    [Fact]
    public async Task DeleteProfileAsync_AnonymizesAndDeactivatesTheAccount()
    {
        var harness = await CreateHarnessAsync();
        var user = (await harness.UserManager.FindByIdAsync(harness.Student.AppUserId.ToString()))!;
        user.PicturePath = "/api/uploads/profile-pictures/student.png";
        await harness.UserManager.UpdateAsync(user);
        var stampBefore = user.SecurityStamp;
        var service = CreateService(harness);

        await DeleteAsync(service);

        var closed = (await harness.UserManager.FindByIdAsync(harness.Student.AppUserId.ToString()))!;
        Assert.Equal("Obrisani", closed.FirstName);
        Assert.Equal("korisnik", closed.LastName);
        Assert.Equal($"obrisan-{closed.Id}", closed.UserName);
        Assert.Equal($"obrisan-{closed.Id}@enote.invalid", closed.Email);
        Assert.Null(closed.PhoneNumber);
        Assert.Null(closed.DateOfBirth);
        Assert.Null(closed.PasswordHash);
        Assert.Null(closed.PicturePath);
        Assert.False(closed.IsActive);
        Assert.NotEqual(stampBefore, closed.SecurityStamp);
        Assert.Equal(["/api/uploads/profile-pictures/student.png"], harness.FileStorage.DeletedPaths);
        Assert.False(await harness.UserManager.CheckPasswordAsync(closed, "Password1!"));
    }

    [Fact]
    public async Task DeleteProfileAsync_CancelsPendingRequestsAndEnrollments_AndKeepsRecords()
    {
        var harness = await CreateHarnessAsync();
        var activeEnrollment = await SeedActiveEnrollmentAsync(harness);
        var pendingEnrollment = await SeedEnrollmentAsync(harness, EnrollmentStatus.Pending);
        var completedEnrollment = await SeedEnrollmentAsync(harness, EnrollmentStatus.Completed);
        harness.Context.Set<CoursePayment>().Add(new CoursePayment(
            activeEnrollment.Id,
            "pi_history",
            10000,
            "BAM",
            PaymentStatus.Succeeded));
        var pendingRental = await SeedRentalAsync(harness, completed: false, paid: false);
        var completedRental = await SeedRentalAsync(harness, completed: true, paid: true);
        harness.Context.Set<Notification>().Add(new Notification(
            harness.Student.AppUserId,
            "Naslov",
            "Tekst",
            Now.AddDays(-1)));
        harness.Context.Set<InstrumentView>().Add(new InstrumentView(
            harness.Student.AppUserId,
            pendingRental.InstrumentId,
            Now.AddDays(-1)));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness);

        await DeleteAsync(service);

        var enrollments = await harness.Context.Set<Enrollment>().ToListAsync();
        Assert.Equal(EnrollmentStatus.Canceled, enrollments.Single(x => x.Id == activeEnrollment.Id).EnrollmentStatus);
        Assert.Equal(EnrollmentStatus.Canceled, enrollments.Single(x => x.Id == pendingEnrollment.Id).EnrollmentStatus);
        Assert.Equal(EnrollmentStatus.Completed, enrollments.Single(x => x.Id == completedEnrollment.Id).EnrollmentStatus);

        var rentals = await harness.Context.Set<InstrumentRental>().ToListAsync();
        var canceled = rentals.Single(x => x.Id == pendingRental.Id);
        Assert.Equal(InstrumentRentalStatus.Canceled, canceled.RentalStatus);
        Assert.Equal(Messages.ProfileDeletedRentalCancelNote, canceled.Note);
        Assert.Equal(InstrumentRentalStatus.Completed, rentals.Single(x => x.Id == completedRental.Id).RentalStatus);

        Assert.Equal(1, await harness.Context.Set<CoursePayment>().CountAsync());
        Assert.Empty(await harness.Context.Set<Notification>().ToListAsync());
        Assert.Empty(await harness.Context.Set<InstrumentView>().ToListAsync());
        Assert.Equal(1, await harness.Context.Set<Student>().CountAsync());
    }

    private static Task DeleteAsync(UserSelfService service) =>
        service.DeleteProfileAsync(new DeleteProfileRequest { CurrentPassword = "Password1!" });

    private static UserSelfService CreateService(Harness harness) =>
        new(
            harness.AccountService,
            harness.Actor,
            harness.Context,
            new FixedClock(Now),
            harness.Gateway,
            harness.FileStorage,
            NullLogger<UserSelfService>.Instance);

    private static async Task<Harness> CreateHarnessAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);
        var fileStorage = new RecordingFileStorageService();
        var accountService = new UserAccountService(identity.UserManager, fileStorage);
        var user = new AppUser
        {
            UserName = "student",
            Email = "student@enote.com",
            FirstName = "Amar",
            LastName = "Amić",
            PhoneNumber = "061111222",
            DateOfBirth = new DateTime(2001, 3, 4),
            IsActive = true
        };
        await identity.UserManager.CreateAsync(user, "Password1!");

        var student = new Student(user.Id, Now.AddMonths(-2));
        student.UpdateMembership(Now.AddMonths(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: student);
        return new Harness(context, identity.UserManager, fileStorage, accountService, new FakePaymentGateway(), actor, student);
    }

    private static async Task<Enrollment> SeedActiveEnrollmentAsync(Harness harness) =>
        await SeedEnrollmentAsync(harness, EnrollmentStatus.Active);

    private static async Task<Enrollment> SeedEnrollmentAsync(Harness harness, EnrollmentStatus status)
    {
        var instructor = new Instructor(1000 + await harness.Context.Set<Instructor>().CountAsync());
        harness.Context.Set<Instructor>().Add(instructor);
        await harness.Context.SaveChangesAsync();

        var course = new Course(
            $"Gitara {status}",
            null,
            100m,
            Now.AddMonths(-1),
            Now.AddMonths(6),
            instructor.Id);
        course.SetPublishedStatus(true);
        harness.Context.Set<Course>().Add(course);
        await harness.Context.SaveChangesAsync();

        var enrollment = new Enrollment(harness.Student.Id, course.Id, status);

        if (status == EnrollmentStatus.Active)
        {
            enrollment.ExtendPaidUntil(Now, 30);
        }

        harness.Context.Set<Enrollment>().Add(enrollment);
        await harness.Context.SaveChangesAsync();

        return enrollment;
    }

    private static async Task<Instrument> SeedInstrumentAsync(Harness harness) =>
        await harness.Context.Set<Instrument>().FirstOrDefaultAsync()
        ?? await RentalTestData.SeedInstrumentAsync(harness.Context);

    private static async Task<InstrumentRental> SeedRentalAsync(Harness harness, bool completed, bool paid)
    {
        var instrument = await SeedInstrumentAsync(harness);
        var rental = new InstrumentRental(
            instrument.Id,
            harness.Student.Id,
            instrument.MusicStoreId,
            Now.AddDays(-10),
            null);

        if (completed)
        {
            rental.Approve(50m, null, Now.AddDays(-9), 1);
            rental.Pickup(Now.AddDays(-9));
            rental.Complete(Now.AddDays(-3), null);
        }

        if (paid)
        {
            rental.MarkPaid(5000, Now.AddDays(-2));
        }

        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        return rental;
    }

    private sealed record Harness(
        ENoteContext Context,
        UserManager<AppUser> UserManager,
        RecordingFileStorageService FileStorage,
        UserAccountService AccountService,
        FakePaymentGateway Gateway,
        StubCurrentActor Actor,
        Student Student);
}
