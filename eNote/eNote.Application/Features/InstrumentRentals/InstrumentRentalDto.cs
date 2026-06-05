using eNote.Domain.Enums;

namespace eNote.Application.Features.InstrumentRentals
{
    public class InstrumentRentalDto
    {
        public int Id { get; set; }

        public int InstrumentId { get; set; }
        public string InstrumentModel { get; set; } = null!;
        public string InstrumentType { get; set; } = null!;

        public int MusicStoreId { get; set; }
        public string StoreName { get; set; } = null!;

        public int StudentProfileId { get; set; }

        public decimal Fee { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public InstrumentRentalStatus RentalStatus { get; set; }
        public string? Note { get; set; }

        public int? MonthsCharged { get; set; }      
        public int? DaysCharged { get; set; }        
        public decimal? DailyFee { get; set; }       
        public bool IsProrated { get; set; }         
        public decimal? TotalFee { get; set; }
    }
}
