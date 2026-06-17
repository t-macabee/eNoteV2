using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Instructor : AuditableEntity
    {
        public int AppUserId
        {
            get; private set;
        }

        protected Instructor()
        {
        }

        public Instructor(int appUserId)
        {
            AppUserId = appUserId;
        }

        public ICollection<Course> Courses { get; private set; } = new List<Course>();
    }
}
