namespace eNote.Domain.Entities
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }
        public int? Grade { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? FilePath { get; set; } 

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}
