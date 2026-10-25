namespace eNote.Domain.Entities.Identity;

public sealed class MembershipPayment : AuditableEntity
{
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateTime PaidAt { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string ReceiptNumber { get; private set; } = null!;
    public string? Note { get; private set; }

    private MembershipPayment()
    {
    }

    public MembershipPayment(
        int studentId,
        decimal amount,
        DateTime paidAt,
        DateTime periodStart,
        DateTime periodEnd,
        string receiptNumber,
        string? note)
    {
        StudentId = studentId;
        Amount = amount;
        PaidAt = paidAt;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        ReceiptNumber = receiptNumber.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
