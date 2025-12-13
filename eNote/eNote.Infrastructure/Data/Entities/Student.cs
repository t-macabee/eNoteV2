using eNote.Infrastructure.Identity;

namespace eNote.Infrastructure.Data.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public DateTime EnrollmentDate { get; set; }
    }
}
