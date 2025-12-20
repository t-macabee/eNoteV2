using eNote.Domain.Entities.Base;
using eNote.Domain.Entities.Lectures;
using eNote.Domain.Entities.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace eNote.Domain.Entities.Courses
{
    public class Course : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [NotMapped]
        public int? EnrolledCount => Enrollments.Count;

        public int InstructorId { get; set; }
        public Instructor Instructor { get; set; } = null!;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
    }
}
