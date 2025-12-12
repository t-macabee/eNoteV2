using eNote.Infrastructure.Identity;

namespace eNote.Infrastructure.Persistence.Entities
{
    public class Instructor
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
