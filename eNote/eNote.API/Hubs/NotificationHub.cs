using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;

namespace eNote.API.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public const string HubPath = "/hubs/notifications";
    public const string ReceiveMethod = "ReceiveNotification";

    public static string UserGroup(int userId) => $"user:{userId}";

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdValue = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdValue, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }
        else
        {
            _logger.LogWarning("NotificationHub connection {ConnectionId} has no valid 'Sub' claim; user will not receive real-time notifications.", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
