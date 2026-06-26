namespace eNote.Application.Features.Identity.Instructors;

public class InstructorDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
}
