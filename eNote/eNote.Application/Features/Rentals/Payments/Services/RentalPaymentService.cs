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
        return await context.ExecuteInTransactionAsync(async () =>
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
                rental.Id,
                "rental",
                existingRows,
                p => p.StripePaymentIntentId,
                p => (p.AmountChargedCents, p.Currency, p.Status),
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
                intentId => context.Set<RentalPayment>().FirstAsync(p => p.StripePaymentIntentId == intentId, cancellationToken),
                cancellationToken);

            return new CreatePaymentIntentResponse(rental.Id, result.PaymentIntentId, result.ClientSecret, result.AmountCents, result.Currency, result.Status);
        }, cancellationToken);
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
        return await context.ExecuteInTransactionAsync(async () =>
        {
            var rental = await LoadForStoreAsync(rentalId, cancellationToken);

            var payment = await context.Set<RentalPayment>()
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

            payment.ApplyRefund(refund.AmountCents, refund.Id, clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);

            var dto = mapper.Map<InstrumentRentalDto>(rental);
            dto.ApplyCharges(rental, rental.CalculateCharges(clock.UtcNow));
            await notificationDispatcher.DispatchPaymentRefundedAsync(dto, refund.AmountCents, payment.Currency, currentUser.UserId);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Refunded {AmountCents} {Currency} on PaymentIntent {PaymentIntentId} for rental {RentalId}", refund.AmountCents, payment.Currency, payment.StripePaymentIntentId, rental.Id);

            return mapper.Map<RentalPaymentDto>(payment);
        }, cancellationToken);
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
