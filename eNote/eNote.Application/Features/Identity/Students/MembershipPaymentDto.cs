namespace eNote.Application.Features.Identity.Students;

public sealed record MembershipPaymentDto(
    int Id,
    int StudentId,
    decimal Amount,
    DateTime PaidAt,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string ReceiptNumber,
    string? Note);
