namespace eNote.Domain.Entities.Rentals;

public class Manufacturer : BaseEntity
{
    public const int NameMaxLength = 100;

    public string Name { get; set; } = null!;
}
