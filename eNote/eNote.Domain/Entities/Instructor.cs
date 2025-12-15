namespace eNote.Domain.Entities
{
    public class Instructor
    {
        public int Id { get; set; }        
        public int AppUserId { get; set; }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
