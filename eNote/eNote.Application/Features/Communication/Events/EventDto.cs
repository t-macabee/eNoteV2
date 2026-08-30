namespace eNote.Application.Features.Communication.Events;

public sealed class EventDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
    public int? AddressId { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressCity { get; init; }
    public int? CourseId { get; init; }
    public string? CourseName { get; init; }
    public int? InstructorId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
