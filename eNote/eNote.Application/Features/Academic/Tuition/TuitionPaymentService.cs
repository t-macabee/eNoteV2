using eNote.Application.Common.Localization;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Tuition;

public sealed class TuitionPaymentService(
    IAppDbContext context,
    IClock clock,
    IStudentContext students,
    IPaymentGateway paymentGateway,
    StripeOptions options,
    ILogger<TuitionPaymentService> logger) : ITuitionPaymentService
{
    private static readonly TimeSpan RequiresActionReuseWindow = TimeSpan.FromMinutes(30);

    public async Task<CreateTuitionIntentResponse> CreateIntentAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        return await context.ExecuteInTransactionAsync(async () =>
        {
            var enrollment = await LoadForStudentAsync(enrollmentId, cancellationToken);

            if (enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            {
                throw new BusinessException(Messages.StudentNotEnrolled);
            }

            if (enrollment.Course.Price == 0)
            {
                throw new BusinessException(Messages.CourseIsFree);
            }

            var existing = await context.Set<CoursePayment>()
                .Where(p => p.EnrollmentId == enrollment.Id
                    && p.Status == PaymentStatus.RequiresAction
                    && p.CreatedAt >= clock.UtcNow - RequiresActionReuseWindow)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                logger.LogInformation("Reusing requires-action PaymentIntent {PaymentIntentId} for enrollment {EnrollmentId}", existing.StripePaymentIntentId, enrollment.Id);

                var current = await InvokeGatewayAsync(
                    () => paymentGateway.RetrievePaymentIntentAsync(existing.StripePaymentIntentId, cancellationToken),
                    cancellationToken);

                return new CreateTuitionIntentResponse(
                    enrollment.Id,
                    current.Id,
                    current.ClientSecret,
                    current.AmountCents,
                    current.Currency,
                    MapStatus(current.Status));
            }

            var amountCents = (long)Math.Round(enrollment.Course.Price * 100m, MidpointRounding.AwayFromZero);
            var currency = options.Currency.Trim().ToLowerInvariant();
            var paidUntilKey = enrollment.PaidUntil.HasValue ? enrollment.PaidUntil.Value.ToString("O") : "none";
            var idempotencyKey = $"tuition:{enrollment.Id}:{paidUntilKey}:v1";

            var metadata = new Dictionary<string, string>
            {
                ["enrollmentId"] = enrollment.Id.ToString(),
                ["courseId"] = enrollment.CourseId.ToString(),
                ["studentId"] = enrollment.StudentId.ToString()
            };

            var intent = await InvokeGatewayAsync(
                () => paymentGateway.CreatePaymentIntentAsync(
                    amountCents,
                    currency,
                    metadata,
                    idempotencyKey,
                    TuitionOptions.StatementDescriptorSuffix,
                    cancellationToken),
                cancellationToken);

            var payment = new CoursePayment(
                enrollment.Id,
                intent.Id,
                intent.AmountCents,
                intent.Currency,
                MapStatus(intent.Status));

            context.Set<CoursePayment>().Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created PaymentIntent {PaymentIntentId} for enrollment {EnrollmentId} ({AmountCents} {Currency})", intent.Id, enrollment.Id, intent.AmountCents, intent.Currency);

            return new CreateTuitionIntentResponse(
                enrollment.Id,
                intent.Id,
                intent.ClientSecret,
                intent.AmountCents,
                intent.Currency,
                payment.Status);
        }, cancellationToken);
    }

    public async Task<CoursePaymentDto> GetLatestAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await LoadForStudentAsync(enrollmentId, cancellationToken);

        var payment = await context.Set<CoursePayment>()
            .Where(p => p.EnrollmentId == enrollment.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.TuitionPaymentNotFound);

        return MapPayment(payment);
    }

    public async Task<IReadOnlyList<CoursePaymentDto>> GetHistoryAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await LoadForStudentAsync(enrollmentId, cancellationToken);

        var payments = await context.Set<CoursePayment>()
            .Where(p => p.EnrollmentId == enrollment.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(MapPayment).ToList();
    }

    private async Task<Enrollment> LoadForStudentAsync(int enrollmentId, CancellationToken cancellationToken)
    {
        var studentId = await students.GetCurrentStudentIdAsync();
        var enrollment = await context.Set<Enrollment>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken)
            ?? throw new NotFoundException(Messages.EnrollmentNotFound);

        if (enrollment.StudentId != studentId)
        {
            throw new NotFoundException(Messages.EnrollmentNotFound);
        }

        return enrollment;
    }

    private async Task<T> InvokeGatewayAsync<T>(Func<Task<T>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is not AppException and not OperationCanceledException)
        {
            logger.LogError(ex, "Stripe gateway invocation failed: {Message}", ex.Message);
            throw new PaymentProviderUnavailableException(Messages.PaymentProviderUnavailable);
        }
    }

    private static PaymentStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "succeeded" => PaymentStatus.Succeeded,
        "canceled" => PaymentStatus.Canceled,
        "requires_payment_method" or "requires_action" or "requires_confirmation" or "requires_capture" or "processing" => PaymentStatus.RequiresAction,
        _ => PaymentStatus.Failed
    };

    private static CoursePaymentDto MapPayment(CoursePayment payment) => new(
        payment.Id,
        payment.EnrollmentId,
        payment.StripePaymentIntentId,
        payment.AmountChargedCents,
        payment.Currency,
        payment.Status,
        payment.PaidAt,
        payment.PeriodStart,
        payment.PeriodEnd);
}
