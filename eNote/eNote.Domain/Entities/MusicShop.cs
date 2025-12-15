namespace eNote.Domain.Entities
{
    public class MusicShop
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = null!;
        public string BusinessHours { get; set; } = null!;

        public int AppUserId { get; set; }
    }
}
