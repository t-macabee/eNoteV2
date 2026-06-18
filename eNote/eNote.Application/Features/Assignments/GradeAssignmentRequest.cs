using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Assignments
{
    public class GradeAssignmentRequest
    {
        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
        public int Grade { get; set; }
    }
}
