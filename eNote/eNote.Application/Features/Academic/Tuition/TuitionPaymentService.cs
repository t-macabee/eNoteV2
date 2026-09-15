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
    public async Task<CreateTuitionIntentResponse> CreateIntentAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        return await context.ExecuteInTransactionAsync(async () =>
        {
            var enrollment = await LoadForStudentAsync(enrollmentId, cancellationToken);

            if (enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            {
                throw new BusinessException(Messages.StudentNotEnrolled);
            }

            // Deliberately no "already paid" refusal: a paid enrollment may buy the next period early, and
            // ExtendPaidUntil stacks the new period on top of the existing PaidUntil.
            if (enrollment.Course.Price == 0)
            {
                throw new BusinessException(Messages.CourseIsFree);
            }

            var existing = await context.Set<CoursePayment>()
                .Where(p => p.EnrollmentId == enrollment.Id
                    && p.Status == PaymentStatus.RequiresAction
                    && p.CreatedAt >= clock.UtcNow - PaymentGatewayHelpers.RequiresActionReuseWindow)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                logger.LogInformation("Reusing requires-action PaymentIntent {PaymentIntentId} for enrollment {EnrollmentId}", existing.StripePaymentIntentId, enrollment.Id);

                var current = await PaymentGatewayHelpers.InvokeGatewayAsync(
                    logger,
                    () => paymentGateway.RetrievePaymentIntentAsync(existing.StripePaymentIntentId, cancellationToken));

                return new CreateTuitionIntentResponse(
                    enrollment.Id,
                    current.Id,
                    current.ClientSecret,
                    current.AmountCents,
                    current.Currency,
                    PaymentGatewayHelpers.MapStatus(current.Status));
            }

            var amountCents = PaymentGatewayHelpers.ToCents(enrollment.Course.Price);
            var currency = PaymentGatewayHelpers.NormalizeCurrency(options.Currency);
            var paidUntilKey = enrollment.PaidUntil.HasValue ? enrollment.PaidUntil.Value.ToString("O") : "none";
            var idempotencyKey = $"tuition:{enrollment.Id}:{paidUntilKey}:v1";

            var metadata = new Dictionary<string, string>
            {
                ["enrollmentId"] = enrollment.Id.ToString(),
                ["courseId"] = enrollment.CourseId.ToString(),
                ["studentId"] = enrollment.StudentId.ToString()
            };

            var intent = await PaymentGatewayHelpers.InvokeGatewayAsync(
                logger,
                () => paymentGateway.CreatePaymentIntentAsync(
                    amountCents,
                    currency,
                    metadata,
                    idempotencyKey,
                    TuitionOptions.StatementDescriptorSuffix,
                    cancellationToken));

            var payment = new CoursePayment(
                enrollment.Id,
                intent.Id,
                intent.AmountCents,
                intent.Currency,
                PaymentGatewayHelpers.MapStatus(intent.Status));

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
