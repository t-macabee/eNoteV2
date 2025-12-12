using eNote.Infrastructure.Identity;

namespace eNote.Infrastructure.Persistence.Entities
{
    public class MusicShop
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public string StoreName { get; set; } = null!;
        public string BusinessHours { get; set; } = null!;
    }
}
