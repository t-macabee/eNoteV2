using eNote.Infrastructure.Identity;

namespace eNote.Infrastructure.Data.Entities
{
    public class Instructor
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
