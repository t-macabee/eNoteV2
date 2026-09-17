using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Tuition;

public sealed class TuitionPaymentService(
    IAppDbContext context,
    IMapper mapper,
    IClock clock,
    IStudentContext students,
    IPaymentGateway paymentGateway,
    StripeOptions options,
    ILogger<TuitionPaymentService> logger)
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

            if (!enrollment.Course.IsPublished || enrollment.Course.EndDate < clock.UtcNow)
            {
                throw new BusinessException(Messages.CourseNotPayable);
            }

            var existingRows = context.Set<CoursePayment>()
                .Where(p => p.EnrollmentId == enrollment.Id
                    && p.Status == PaymentStatus.RequiresAction
                    && p.CreatedAt >= clock.UtcNow - PaymentGatewayHelpers.RequiresActionReuseWindow)
                .OrderByDescending(p => p.CreatedAt);

            var result = await PaymentGatewayHelpers.ReuseOrCreatePaymentIntentAsync(
                logger,
                context,
                paymentGateway,
                enrollment.Id,
                "enrollment",
                existingRows,
                p => p.StripePaymentIntentId,
                p => (p.AmountChargedCents, p.Currency, p.Status),
                async () =>
                {
                    var amountCents = PaymentGatewayHelpers.ToCents(enrollment.Course.Price);
                    var paidUntilKey = enrollment.PaidUntil.HasValue ? enrollment.PaidUntil.Value.ToString("O") : "none";
                    var attempt = await context.Set<CoursePayment>().CountAsync(p => p.EnrollmentId == enrollment.Id, cancellationToken);
                    var idempotencyKey = $"tuition:{enrollment.Id}:{paidUntilKey}:{attempt}:v2";

                    var metadata = new Dictionary<string, string>
                    {
                        ["enrollmentId"] = enrollment.Id.ToString(),
                        ["courseId"] = enrollment.CourseId.ToString(),
                        ["studentId"] = enrollment.StudentId.ToString()
                    };

                    return new PaymentIntentCreation(amountCents, options.Currency, metadata, idempotencyKey, TuitionOptions.StatementDescriptorSuffix);
                },
                intent => new CoursePayment(
                    enrollment.Id,
                    intent.Id,
                    intent.AmountCents,
                    intent.Currency,
                    PaymentGatewayHelpers.MapStatus(intent.Status)),
                DbConstraintNames.CoursePaymentPaymentIntentIdUniqueIndex,
                intentId => context.Set<CoursePayment>().FirstAsync(p => p.StripePaymentIntentId == intentId, cancellationToken),
                cancellationToken);

            return new CreateTuitionIntentResponse(
                enrollment.Id,
                result.PaymentIntentId,
                result.ClientSecret,
                result.AmountCents,
                result.Currency,
                result.Status);
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

        return mapper.Map<CoursePaymentDto>(payment);
    }

    public async Task<IReadOnlyList<CoursePaymentDto>> GetHistoryAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await LoadForStudentAsync(enrollmentId, cancellationToken);

        var payments = await context.Set<CoursePayment>()
            .Where(p => p.EnrollmentId == enrollment.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<CoursePaymentDto>>(payments);
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
}
