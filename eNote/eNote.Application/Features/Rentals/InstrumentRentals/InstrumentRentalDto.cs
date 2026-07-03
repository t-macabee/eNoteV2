namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class InstrumentRentalDto
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public int MusicStoreId { get; set; }
    public int StudentProfileId { get; set; }
    public int StudentUserId { get; set; }

    public string InstrumentModel { get; set; } = null!;
    public string InstrumentType { get; set; } = null!;
    public string StoreName { get; set; } = null!;
    public InstrumentRentalStatus RentalStatus { get; set; }
    public string? RequestNote { get; set; }
    public string? Note { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public int? ApprovedById { get; set; }
    public int? RejectedById { get; set; }

    public decimal Fee { get; set; }
    public decimal? DailyFee { get; set; }
    public int? MonthsCharged { get; set; }
    public int? DaysCharged { get; set; }
    public bool IsProrated { get; set; }
    public decimal? TotalFee { get; set; }
}
