using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Instrument
    {
        public int Id { get; set; }
        public string Model { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string? Description { get; set; }
        public byte[]? Image { get; set; }
        public bool IsAvailable => !InstrumentRentals.Any(x => x.RentalStatus == InstrumentRentalStatus.Approved);

        public int InstrumentTypeId { get; set; }
        public InstrumentType InstrumentType { get; set; } = null!;
        public int MusicShopId { get; set; }
        public MusicShop MusicShop { get; set; } = null!;

        public ICollection<InstrumentRental> InstrumentRentals { get; set; } = new List<InstrumentRental>();
    }
}
