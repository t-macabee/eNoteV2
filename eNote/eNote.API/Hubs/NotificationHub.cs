using eNote.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;

namespace eNote.API.Hubs;

[Authorize(Roles = AppRoles.Student)]
public sealed class NotificationHub : Hub
{
    public const string HubPath = "/hubs/notifications";
    public const string ReceiveMethod = "ReceiveNotification";

    public static string UserGroup(int userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userIdValue = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdValue, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }
}
