namespace eNote.Domain.Entities.Communication;

public class Event : AuditableEntity
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }

    public int? AddressId { get; private set; }
    public Address? Address { get; private set; }

    protected Event()
    {
    }

    public Event(string title, string description, DateTime startsAt, DateTime? endsAt, int? addressId)
    {
        Title = title;
        Description = description;
        StartsAt = startsAt;
        EndsAt = endsAt;
        AddressId = addressId;
    }

    public void UpdateDetails(string title, string description, DateTime startsAt, DateTime? endsAt, int? addressId)
    {
        Title = title;
        Description = description;
        StartsAt = startsAt;
        EndsAt = endsAt;
        AddressId = addressId;
    }
}
