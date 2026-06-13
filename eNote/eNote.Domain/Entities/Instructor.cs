using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Instructor : AuditableEntity
    {
        public int AppUserId { get; set; }

        protected Instructor() { }

        public Instructor(int appUserId)
        {
            AppUserId = appUserId;
        }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
