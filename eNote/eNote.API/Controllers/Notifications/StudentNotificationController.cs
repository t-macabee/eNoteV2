using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Notifications;
using eNote.Application.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Notifications
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/notifications")]
    public sealed class StudentNotificationController(INotificationService notificationService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetPaged([FromQuery] NotificationSearchObject search)
        {
            PagedResult<NotificationDto> result = await notificationService.GetPagedAsync(search);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<NotificationUnreadCountDto>> GetUnreadCount()
        {
            NotificationUnreadCountDto result = await notificationService.GetUnreadCountAsync();
            return Ok(result);
        }

        [HttpPatch("{id:int}/read")]
        public async Task<ActionResult<NotificationDto>> MarkRead(int id)
        {
            NotificationDto result = await notificationService.MarkReadAsync(id);
            return Ok(result);
        }

        [HttpPatch("read-all")]
        public async Task<ActionResult<NotificationUnreadCountDto>> MarkAllRead()
        {
            NotificationUnreadCountDto result = await notificationService.MarkAllReadAsync();
            return Ok(result);
        }
    }
}
