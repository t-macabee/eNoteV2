using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Payments.Services;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserSelfService(
    IUserAccountService accountService,
    ICurrentUserContext currentUserService,
    IAppDbContext context,
    IClock clock,
    IPaymentGateway paymentGateway,
    IFileStorageService fileStorage,
    RentalCommandService rentalCommands,
    ILogger<UserSelfService> logger)
{
    public Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
        => accountService.UpdateExistingUserAsync(currentUserService.UserId, request.Email, request.FirstName, request.LastName, request.DateOfBirth, cancellationToken);

    public Task UpdatePictureAsync(Stream picture, string contentType, CancellationToken cancellationToken)
        => accountService.UpdatePictureAsync(currentUserService.UserId, picture, contentType, cancellationToken);

    public Task<(Stream? Data, string? ContentType)> GetPictureAsync(CancellationToken cancellationToken = default)
        => accountService.GetPictureAsync(currentUserService.UserId, cancellationToken);

    public Task DeletePictureAsync(CancellationToken cancellationToken = default)
        => accountService.DeletePictureAsync(currentUserService.UserId, cancellationToken);

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        => accountService.ChangePasswordAsync(currentUserService.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);

    public async Task DeleteProfileAsync(DeleteProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;

        if (!await accountService.VerifyPasswordAsync(userId, request.CurrentPassword, cancellationToken))
        {
            throw new CurrentPasswordInvalidException();
        }

        var student = await context.Set<Student>().FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken)
            ?? throw new NotFoundException(Messages.StudentProfileNotFound);

        await EnsureNoBlockingRentalsAsync(student.Id, cancellationToken);

        await ResolveOpenPaymentsAsync(student.Id, cancellationToken);

        string? picturePath;

        try
        {
            picturePath = await context.ExecuteInTransactionAsync(async () =>
            {
                await EnsureNoOpenRentalsAsync(student.Id, cancellationToken);
                await rentalCommands.CancelPendingForClosedAccountAsync(student.Id, userId, cancellationToken);
                await CancelEnrollmentsAsync(student.Id, userId, cancellationToken);
                await DeletePersonalDataAsync(userId, cancellationToken);

                await context.SaveChangesAsync(cancellationToken);

                return await accountService.CloseAccountAsync(userId, cancellationToken);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }

        if (!string.IsNullOrWhiteSpace(picturePath))
        {
            fileStorage.Delete(picturePath);
        }

        logger.LogInformation("Student {StudentUserId} closed the profile", userId);
    }

    private async Task ResolveOpenPaymentsAsync(int studentId, CancellationToken cancellationToken)
    {
        var openAttempts = new List<StripePayment>();

        openAttempts.AddRange(await context.Set<RentalPayment>()
            .Where(p => p.InstrumentRental.StudentProfileId == studentId
                && StripePayment.OpenStatuses.Contains(p.Status))
            .ToListAsync(cancellationToken));

        openAttempts.AddRange(await context.Set<CoursePayment>()
            .Where(p => p.Enrollment.StudentId == studentId
                && StripePayment.OpenStatuses.Contains(p.Status))
            .ToListAsync(cancellationToken));

        await PaymentGatewayHelpers.ResolveOpenAttemptsAsync(
            logger,
            context,
            paymentGateway,
            openAttempts,
            expected: null,
            Messages.PaymentProcessing,
            cancellationToken);
    }

    private async Task EnsureNoBlockingRentalsAsync(int studentId, CancellationToken cancellationToken)
    {
        var reasons = new List<string>();

        if (await context.Set<InstrumentRental>()
                .Where(r => r.StudentProfileId == studentId)
                .WhereUnpaidDebt()
                .AnyAsync(cancellationToken))
        {
            reasons.Add(Messages.ProfileDeleteBlockedByUnpaidRental);
        }

        if (await HasOpenRentalAsync(studentId, cancellationToken))
        {
            reasons.Add(Messages.ProfileDeleteBlockedByOpenRental);
        }

        if (reasons.Count > 0)
        {
            throw new BusinessException(string.Join(' ', reasons));
        }
    }

    private async Task EnsureNoOpenRentalsAsync(int studentId, CancellationToken cancellationToken)
    {
        if (await HasOpenRentalAsync(studentId, cancellationToken))
        {
            throw new BusinessException(Messages.ProfileDeleteBlockedByOpenRental);
        }
    }

    private Task<bool> HasOpenRentalAsync(int studentId, CancellationToken cancellationToken) =>
        context.Set<InstrumentRental>().AnyAsync(r => r.StudentProfileId == studentId
            && (r.RentalStatus == InstrumentRentalStatus.Approved || r.RentalStatus == InstrumentRentalStatus.Active),
            cancellationToken);

    private async Task CancelEnrollmentsAsync(int studentId, int userId, CancellationToken cancellationToken)
    {
        var enrollments = await context.Set<Enrollment>()
            .Where(e => e.StudentId == studentId
                && (e.EnrollmentStatus == EnrollmentStatus.Pending || e.EnrollmentStatus == EnrollmentStatus.Active))
            .ToListAsync(cancellationToken);

        foreach (var enrollment in enrollments)
        {
            var result = enrollment.Transition(EnrollmentTrigger.Cancel, userId, clock.UtcNow);

            if (!result.IsSuccess)
            {
                throw new BusinessException(result.Error);
            }
        }
    }

    private async Task DeletePersonalDataAsync(int userId, CancellationToken cancellationToken)
    {
        var notifications = await context.Set<Notification>()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        context.Set<Notification>().RemoveRange(notifications);

        var instrumentViews = await context.Set<InstrumentView>()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        context.Set<InstrumentView>().RemoveRange(instrumentViews);
    }
}
