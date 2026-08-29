namespace eNote.Domain.Entities.Shared;

public class Address : BaseEntity
{
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}
