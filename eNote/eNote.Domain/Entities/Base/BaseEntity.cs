namespace eNote.Domain.Entities.Base
{
    public abstract class BaseEntity : IEntity
    {
        public int Id { get; set; }
    }
}
