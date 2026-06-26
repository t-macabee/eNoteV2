using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Notifications;
using eNote.Application.Features.Communication.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Notifications;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/notifications")]
public sealed class StudentNotificationController(INotificationService notificationService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetPaged([FromQuery] NotificationSearchObject search)
    {
        var result = await notificationService.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(NotificationUnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountDto>> GetUnreadCount()
    {
        var result = await notificationService.GetUnreadCountAsync();
        return Ok(result);
    }

    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationDto>> MarkRead(int id)
    {
        var result = await notificationService.MarkReadAsync(id);
        return Ok(result);
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(NotificationUnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountDto>> MarkAllRead()
    {
        var result = await notificationService.MarkAllReadAsync();
        return Ok(result);
    }
}
