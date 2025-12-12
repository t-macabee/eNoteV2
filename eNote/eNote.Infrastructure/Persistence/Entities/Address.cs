namespace eNote.Infrastructure.Persistence.Entities
{
    public class Address
    {
        public int Id { get; set; }
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string Number { get; set; } = null!;
    }
}
