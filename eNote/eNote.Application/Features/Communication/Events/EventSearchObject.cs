using eNote.Application.Common.Search;

namespace eNote.Application.Features.Communication.Events;

public sealed class EventSearchObject : BaseSearchObject
{
    public string? Title { get; set; }
    public int? CourseId { get; set; }
    public int? InstructorId { get; set; }
    public int? AddressId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
