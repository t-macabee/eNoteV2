namespace eNote.Application.Features.Academic.StudentDirectory;

public sealed class StudentDirectoryEntryDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
}
