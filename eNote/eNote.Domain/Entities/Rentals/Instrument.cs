namespace eNote.Domain.Entities.Rentals;

public class Instrument : AuditableEntity, ITenantScoped
{
    public const int ModelMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public int InstrumentTypeId { get; private set; }
    public InstrumentType InstrumentType { get; private set; } = null!;
    public int MusicStoreId { get; private set; }
    public MusicStore MusicStore { get; private set; } = null!;
    public int ManufacturerId { get; private set; }
    public Manufacturer Manufacturer { get; private set; } = null!;

    public string Model { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImagePath { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<InstrumentRental> InstrumentRentals { get; private set; } = [];

    protected Instrument() { }

    public Instrument(string model, int manufacturerId, string? description, string? imagePath, int instrumentTypeId, int musicStoreId)
    {
        Model = model;
        ManufacturerId = manufacturerId;
        Description = description;
        ImagePath = imagePath;
        InstrumentTypeId = instrumentTypeId;
        MusicStoreId = musicStoreId;
    }

    public void UpdateDetails(string model, int manufacturerId, string? description, string? imagePath, int instrumentTypeId)
    {
        Model = model;
        ManufacturerId = manufacturerId;
        Description = description;
        ImagePath = imagePath;
        InstrumentTypeId = instrumentTypeId;
    }

    public void SoftDelete() => IsActive = false;
}