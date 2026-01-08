using eNote.Domain.Entities.Base;
using eNote.Domain.Entities.Users;

namespace eNote.Domain.Entities
{
    public class AssignmentSubmission : BaseEntity
    {        
        public int? Grade { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? FilePath { get; set; } 

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}
