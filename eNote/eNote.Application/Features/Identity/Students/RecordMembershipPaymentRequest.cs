namespace eNote.Application.Features.Identity.Students;

public class RecordMembershipPaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public int? Months { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
}
