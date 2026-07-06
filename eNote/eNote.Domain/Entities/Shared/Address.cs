namespace eNote.Domain.Entities.Shared;

public class Address : BaseEntity
{
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}
