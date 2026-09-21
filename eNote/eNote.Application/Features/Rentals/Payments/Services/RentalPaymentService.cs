using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed class RentalPaymentService(
    IAppDbContext context,
    IMapper mapper,
    IClock clock,
    ICurrentUserContext currentUser,
    IStoreContext stores,
    IPaymentGateway paymentGateway,
    IRentalNotificationDispatcher notificationDispatcher,
    StripeOptions options,
    ILogger<RentalPaymentService> logger)
{
    public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var rental = await LoadForStudentAsync(rentalId, cancellationToken);

        EnsureCanCreatePaymentIntent(rental);

        var existingRows = context.Set<RentalPayment>()
            .Where(p => p.InstrumentRentalId == rental.Id
                && p.Status == PaymentStatus.RequiresAction
                && p.CreatedAt >= clock.UtcNow - PaymentGatewayHelpers.RequiresActionReuseWindow)
            .OrderByDescending(p => p.CreatedAt);

        var result = await PaymentGatewayHelpers.ReuseOrCreatePaymentIntentAsync(
            logger,
            context,
            paymentGateway,
            $"rental {rental.Id}",
            existingRows,
            async () =>
            {
                var charges = rental.CalculateCharges(rental.ReturnedAt ?? clock.UtcNow);

                if (charges.TotalFee is not decimal totalFee || totalFee <= 0)
                {
                    throw new BusinessException(Messages.PaymentNotPayableInStatus);
                }

                var cents = PaymentGatewayHelpers.ToCents(totalFee);
                var attempt = await context.Set<RentalPayment>().CountAsync(p => p.InstrumentRentalId == rental.Id, cancellationToken);
                var idempotencyKey = $"rental:{rental.Id}:total:{cents}:{rental.ReturnedAt:O}:{attempt}:v3";

                var metadata = new Dictionary<string, string>
                {
                    ["rentalId"] = rental.Id.ToString(),
                    ["storeId"] = rental.MusicStoreId.ToString(),
                    ["studentId"] = rental.StudentProfile.AppUserId.ToString()
                };

                return new PaymentIntentCreation(cents, options.Currency, metadata, idempotencyKey, options.StatementDescriptor);
            },
            intent => new RentalPayment(
                rental.Id,
                rental.MusicStoreId,
                intent.Id,
                intent.AmountCents,
                intent.Currency,
                PaymentGatewayHelpers.MapStatus(intent.Status)),
            DbConstraintNames.RentalPaymentStripePaymentIntentIdUniqueIndex,
            cancellationToken);

        return new CreatePaymentIntentResponse(rental.Id, result.PaymentIntentId, result.ClientSecret, result.AmountCents, result.Currency, result.Status);
    }

    public async Task<RentalPaymentDto> GetPaymentStatusAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var rental = await LoadForStudentAsync(rentalId, cancellationToken);

        var payment = await context.Set<RentalPayment>()
            .Where(p => p.InstrumentRentalId == rental.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.PaymentNotFound);

        return mapper.Map<RentalPaymentDto>(payment);
    }

    public async Task<RentalPaymentDto> GetPaymentStatusForStoreAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var rental = await LoadForStoreAsync(rentalId, cancellationToken);

        var payment = await context.Set<RentalPayment>()
            .Where(p => p.InstrumentRentalId == rental.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.PaymentNotFound);

        return mapper.Map<RentalPaymentDto>(payment);
    }

    public async Task<RentalPaymentDto> RefundAsync(int rentalId, long? amountCents, CancellationToken cancellationToken = default)
    {
        var rental = await LoadForStoreAsync(rentalId, cancellationToken);

        var payment = await context.Set<RentalPayment>()
            .Include(p => p.Refunds)
            .SingleOrDefaultAsync(p => p.InstrumentRentalId == rental.Id && (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.PartiallyRefunded), cancellationToken)
            ?? throw new NotFoundException(Messages.PaymentNotFound);

        var centsToRefund = amountCents ?? payment.AmountChargedCents;
        var remaining = payment.AmountChargedCents - (payment.RefundedCents ?? 0);

        if (centsToRefund <= 0 || centsToRefund > remaining)
        {
            throw new BusinessException(Messages.RefundExceedsCharged);
        }

        var refund = await PaymentGatewayHelpers.InvokeGatewayAsync(
            logger,
            () => paymentGateway.CreateRefundAsync(
                payment.StripePaymentIntentId,
                centsToRefund,
                "requested_by_customer",
                $"refund:{payment.Id}:{payment.RefundedCents ?? 0}:{centsToRefund}",
                cancellationToken));

        if (refund.Status == "pending")
        {
            logger.LogInformation("Refund {RefundId} for payment {PaymentId} is pending; leaving payment unchanged", refund.Id, payment.Id);
            return mapper.Map<RentalPaymentDto>(payment);
        }

        if (refund.Status != "succeeded")
        {
            throw new BusinessException(Messages.RefundFailed);
        }

        await context.ExecuteInTransactionAsync(async () =>
        {
            payment.ApplyRefund(refund.AmountCents, refund.Id, clock.UtcNow);

            var dto = mapper.Map<InstrumentRentalDto>(rental);
            dto.ApplyCharges(rental, rental.CalculateCharges(clock.UtcNow));
            await notificationDispatcher.DispatchPaymentRefundedAsync(dto, refund.AmountCents, payment.Currency, currentUser.UserId);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict during refund for rental {RentalId}", rental.Id);
                throw new ConflictException(Messages.ConcurrencyConflict);
            }
        }, cancellationToken);

        logger.LogInformation("Refunded {AmountCents} {Currency} on PaymentIntent {PaymentIntentId} for rental {RentalId}", refund.AmountCents, payment.Currency, payment.StripePaymentIntentId, rental.Id);

        return mapper.Map<RentalPaymentDto>(payment);
    }

    private async Task<InstrumentRental> LoadForStoreAsync(int rentalId, CancellationToken cancellationToken)
    {
        await stores.GetCurrentStoreIdAsync(cancellationToken);

        return await context.Set<InstrumentRental>()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);
    }

    private async Task<InstrumentRental> LoadForStudentAsync(int rentalId, CancellationToken cancellationToken)
    {
        var rental = await context.Set<InstrumentRental>()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);

        if (rental.StudentProfile.AppUserId != currentUser.UserId)
        {
            throw new BusinessException(Messages.RentalAccessDenied);
        }

        return rental;
    }

    private static void EnsureCanCreatePaymentIntent(InstrumentRental rental)
    {
        if (rental.RentalStatus is not (InstrumentRentalStatus.Completed or InstrumentRentalStatus.ReturnedEarly))
        {
            throw new BusinessException(Messages.PaymentNotPayableInStatus);
        }

        if (rental.IsPaid)
        {
            throw new BusinessException(Messages.PaymentAlreadyCompleted);
        }
    }

}
