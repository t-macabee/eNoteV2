using eNote.Domain.Enums;

namespace eNote.Application.Features.Academic.Tuition;

public sealed record CreateTuitionIntentResponse(
    int EnrollmentId,
    string PaymentIntentId,
    string ClientSecret,
    long AmountCents,
    string Currency,
    PaymentStatus Status);

public sealed record CoursePaymentDto(
    int Id,
    int EnrollmentId,
    string PaymentIntentId,
    long AmountCents,
    string Currency,
    PaymentStatus Status,
    DateTime? PaidAt,
    DateTime? PeriodStart,
    DateTime? PeriodEnd);
