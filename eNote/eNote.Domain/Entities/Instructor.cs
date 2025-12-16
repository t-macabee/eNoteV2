using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Instructor : BaseEntity
    { 
        public int AppUserId { get; set; }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
