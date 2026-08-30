namespace eNote.Application.Features.Communication.Events;

public sealed class EventRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int? AddressId { get; set; }
    public int? CourseId { get; set; }
    public int? InstructorId { get; set; }
}
