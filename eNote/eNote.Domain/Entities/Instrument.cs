using eNote.Domain.Entities.Base;
using eNote.Domain.Entities.Users;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Instrument : BaseEntity
    {
        public string Model { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsAvailable => 
            IsActive && !InstrumentRentals.Any(x => x.RentalStatus == InstrumentRentalStatus.Approved);

        public int InstrumentTypeId { get; set; }
        public InstrumentType InstrumentType { get; set; } = null!;
        public int MusicShopId { get; set; }
        public MusicShop MusicShop { get; set; } = null!;

        public ICollection<InstrumentRental> InstrumentRentals { get; set; } = new List<InstrumentRental>();
    }    
}
