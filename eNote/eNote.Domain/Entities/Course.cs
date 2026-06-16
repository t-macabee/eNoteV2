using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Course : AuditableEntity
    {
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public decimal Price { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public bool IsPublished { get; private set; }
        public bool IsActive { get; private set; } = true;

        public int InstructorId { get; private set; }
        public Instructor Instructor { get; private set; } = null!;

        public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();
        public ICollection<Lecture> Lectures { get; private set; } = new List<Lecture>();

        protected Course() { }

        public Course(string name, string? description, decimal price, DateTime? startDate, DateTime? endDate, int instructorId)
        {
            Name = name;
            Description = description;
            Price = price;
            StartDate = startDate;
            EndDate = endDate;
            InstructorId = instructorId;
            IsPublished = false;
            IsActive = true;
        }

        public void UpdateDetails(string name, string? description, decimal price, DateTime? startDate, DateTime? endDate)
        {
            Name = name;
            Description = description;
            Price = price;
            StartDate = startDate;
            EndDate = endDate;
        }

        public void SetPublishedStatus(bool isPublished)
        {
            IsPublished = isPublished;
        }

        public void SoftDelete()
        {
            IsActive = false;
            IsPublished = false;
        }
    }
}
