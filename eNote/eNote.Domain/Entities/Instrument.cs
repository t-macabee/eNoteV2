using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Instrument : AuditableEntity
    {
        public int InstrumentTypeId { get; private set; }
        public InstrumentType InstrumentType { get; private set; } = null!;
        public int MusicStoreId { get; private set; }
        public MusicStore MusicStore { get; private set; } = null!;

        public string Model { get; private set; } = null!;
        public string Manufacturer { get; private set; } = null!;
        public string? Description { get; private set; }
        public string? ImagePath { get; private set; }

        public bool IsActive { get; private set; } = true;
        public bool IsAvailable =>
            IsActive && !InstrumentRentals.Any(x =>
            x.RentalStatus == InstrumentRentalStatus.Approved ||
            x.RentalStatus == InstrumentRentalStatus.Active);

        public ICollection<InstrumentRental> InstrumentRentals { get; private set; } = new List<InstrumentRental>();

        protected Instrument()
        {
        }

        public Instrument(string model, string manufacturer, string? description, string? imagePath, int instrumentTypeId, int musicStoreId)
        {
            Model = model;
            Manufacturer = manufacturer;
            Description = description;
            ImagePath = imagePath;
            InstrumentTypeId = instrumentTypeId;
            MusicStoreId = musicStoreId;
            IsActive = true;
        }

        public void UpdateDetails(string model, string manufacturer, string? description, string? imagePath, int instrumentTypeId)
        {
            Model = model;
            Manufacturer = manufacturer;
            Description = description;
            ImagePath = imagePath;
            InstrumentTypeId = instrumentTypeId;
        }

        public void SoftDelete()
        {
            IsActive = false;
        }
    }
}
