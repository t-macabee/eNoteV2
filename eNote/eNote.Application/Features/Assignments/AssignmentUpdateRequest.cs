namespace eNote.Application.Features.Assignments
{
    public class AssignmentUpdateRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime DueAt { get; set; }
    }
}
