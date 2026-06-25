namespace eNote.Domain.Entities.Base;

public abstract class BaseEntity : IEntity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int? CreatedById { get; set; }
    public int? UpdatedById { get; set; }
}
