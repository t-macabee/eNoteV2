namespace eNote.Application.Features.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public int? RentalId { get; set; }

    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
