namespace eNote.Application.Features.Notifications;

public class NotificationPushDto
{
    public int? RentalId { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
