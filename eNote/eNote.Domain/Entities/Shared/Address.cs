namespace eNote.Domain.Entities.Shared;

public class Address : BaseEntity
{
    public const int StreetMaxLength = 100;
    public const int NumberMaxLength = 20;

    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}
