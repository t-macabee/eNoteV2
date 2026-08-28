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
    ILogger<RentalPaymentService> logger) : IRentalPaymentService
{
    private static readonly TimeSpan RequiresActionReuseWindow = TimeSpan.FromMinutes(30);

    public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        return await context.ExecuteInTransactionAsync(async () =>
        {
            var rental = await LoadForStudentAsync(rentalId, cancellationToken);

            await EnsureCanCreatePaymentIntentAsync(rental, cancellationToken);

            var existing = await context.Set<RentalPayment>()
                .Where(p => p.InstrumentRentalId == rental.Id
                    && p.Status == PaymentStatus.RequiresAction
                    && p.CreatedAt >= clock.UtcNow - RequiresActionReuseWindow)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                logger.LogInformation("Reusing requires-action PaymentIntent {PaymentIntentId} for rental {RentalId}", existing.StripePaymentIntentId, rental.Id);

                var current = await InvokeGatewayAsync(
                    () => paymentGateway.RetrievePaymentIntentAsync(existing.StripePaymentIntentId, cancellationToken),
                    cancellationToken);
                return new CreatePaymentIntentResponse(
                    rental.Id,
                    current.Id,
                    current.ClientSecret,
                    current.AmountCents,
                    current.Currency,
                    MapStatus(current.Status));
            }

            var charges = rental.CalculateCharges(rental.ReturnedAt ?? clock.UtcNow);

            if (charges.TotalFee is not decimal totalFee || totalFee <= 0)
            {
                throw new BusinessException(Messages.PaymentNotPayableInStatus);
            }

            var cents = (long)Math.Round(totalFee * 100m, MidpointRounding.AwayFromZero);
            var currency = options.Currency.Trim().ToLowerInvariant();
            var idempotencyKey = $"rental:{rental.Id}:total:{cents}:{rental.ReturnedAt:O}";

            var metadata = new Dictionary<string, string>
            {
                ["rentalId"] = rental.Id.ToString(),
                ["storeId"] = rental.MusicStoreId.ToString(),
                ["studentId"] = rental.StudentProfile.AppUserId.ToString()
            };

            var intent = await InvokeGatewayAsync(
                () => paymentGateway.CreatePaymentIntentAsync(cents, currency, metadata, idempotencyKey, cancellationToken),
                cancellationToken);
            var payment = new RentalPayment(
                rental.Id,
                rental.MusicStoreId,
                intent.Id,
                intent.AmountCents,
                intent.Currency,
                MapStatus(intent.Status));

            context.Set<RentalPayment>().Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created PaymentIntent {PaymentIntentId} for rental {RentalId} ({AmountCents} {Currency})", intent.Id, rental.Id, intent.AmountCents, intent.Currency);

            return new CreatePaymentIntentResponse(rental.Id, intent.Id, intent.ClientSecret, intent.AmountCents, intent.Currency, payment.Status);
        }, cancellationToken);
    }

    public async Task<RentalPaymentDto> GetPaymentStatusAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var rental = await LoadForStudentAsync(rentalId, cancellationToken);

        var payment = await context.Set<RentalPayment>()
            .Where(p => p.InstrumentRentalId == rental.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.PaymentNotFound);

        return Map(payment);
    }

    public async Task<RentalPaymentDto> GetPaymentStatusForStoreAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var storeId = await stores.GetCurrentStoreIdAsync(cancellationToken);
        var rental = await context.Set<InstrumentRental>()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);

        if (rental.MusicStoreId != storeId)
        {
            throw new BusinessException(Messages.RentalAccessDenied);
        }

        var payment = await context.Set<RentalPayment>()
            .Where(p => p.InstrumentRentalId == rental.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.PaymentNotFound);

        return Map(payment);
    }

    public async Task<RentalPaymentDto> RefundAsync(int rentalId, long? amountCents, CancellationToken cancellationToken = default)
    {
        return await context.ExecuteInTransactionAsync(async () =>
        {
            var storeId = await stores.GetCurrentStoreIdAsync(cancellationToken);
            var rental = await context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);

            if (rental.MusicStoreId != storeId)
            {
                throw new BusinessException(Messages.RentalAccessDenied);
            }

            var payment = await context.Set<RentalPayment>()
                .SingleOrDefaultAsync(p => p.InstrumentRentalId == rental.Id && p.Status == PaymentStatus.Succeeded, cancellationToken)
                ?? throw new NotFoundException(Messages.PaymentNotFound);

            var centsToRefund = amountCents ?? payment.AmountChargedCents;
            var remaining = payment.AmountChargedCents - (payment.RefundedCents ?? 0);

            if (centsToRefund <= 0 || centsToRefund > remaining)
            {
                throw new BusinessException(Messages.RefundExceedsCharged);
            }

            var refund = await paymentGateway.CreateRefundAsync(
                payment.StripePaymentIntentId,
                centsToRefund,
                "requested_by_customer",
                $"refund:{payment.Id}:{Guid.NewGuid()}",
                cancellationToken);

            payment.ApplyRefund(refund.AmountCents, refund.Id, clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);

            var dto = mapper.Map<InstrumentRentalDto>(rental);
            dto.ApplyCharges(rental, rental.CalculateCharges(clock.UtcNow));
            await notificationDispatcher.DispatchPaymentRefundedAsync(dto, refund.AmountCents, currentUser.UserId);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Refunded {AmountCents} {Currency} on PaymentIntent {PaymentIntentId} for rental {RentalId}", refund.AmountCents, payment.Currency, payment.StripePaymentIntentId, rental.Id);

            return Map(payment);
        }, cancellationToken);
    }

    private static async Task<T> InvokeGatewayAsync<T>(Func<Task<T>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is not AppException and not OperationCanceledException)
        {
            throw new PaymentProviderUnavailableException(Messages.PaymentProviderUnavailable);
        }
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

    private async Task EnsureCanCreatePaymentIntentAsync(InstrumentRental rental, CancellationToken cancellationToken)
    {
        if (rental.RentalStatus is not (InstrumentRentalStatus.Completed or InstrumentRentalStatus.ReturnedEarly))
        {
            throw new BusinessException(Messages.PaymentNotPayableInStatus);
        }

        if (!rental.PickedUpAt.HasValue)
        {
            throw new BusinessException(Messages.PaymentNotPayableInStatus);
        }

        if (rental.IsPaid)
        {
            throw new BusinessException(Messages.PaymentAlreadyCompleted);
        }
        if (await context.Set<RentalPayment>().AnyAsync(p => p.InstrumentRentalId == rental.Id && p.Status == PaymentStatus.Succeeded, cancellationToken))
        {
            throw new BusinessException(Messages.PaymentAlreadyCompleted);
        }
    }

    private static RentalPaymentDto Map(RentalPayment payment) => new(
        payment.Id,
        payment.InstrumentRentalId,
        payment.StripePaymentIntentId,
        payment.AmountChargedCents,
        payment.Currency,
        payment.Status,
        payment.PaidAt,
        payment.RefundedAt,
        payment.RefundedCents);

    private static PaymentStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "succeeded" => PaymentStatus.Succeeded,
        "canceled" => PaymentStatus.Canceled,
        "requires_payment_method" or "requires_action" or "requires_confirmation" or "requires_capture" or "processing" => PaymentStatus.RequiresAction,
        _ => PaymentStatus.Failed
    };

}
