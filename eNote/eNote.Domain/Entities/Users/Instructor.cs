using eNote.Domain.Entities.Base;
using eNote.Domain.Entities.Courses;

namespace eNote.Domain.Entities.Users
{
    public class Instructor : BaseEntity
    { 
        public int UserId { get; set; }

        protected Instructor() { }

        public Instructor(int userId)         
        {
            UserId = userId;
        }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
