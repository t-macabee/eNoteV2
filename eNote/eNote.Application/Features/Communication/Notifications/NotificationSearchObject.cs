using eNote.Application.Common.Search;

namespace eNote.Application.Features.Communication.Notifications;

public class NotificationSearchObject : BaseSearchObject
{
    public bool? IsRead { get; set; }
}
