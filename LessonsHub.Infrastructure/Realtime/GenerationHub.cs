using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LessonsHub.Infrastructure.Realtime;

/// <summary>
/// Per-user push channel for job lifecycle events. Each connection joins the
/// "user-{userId}" group on connect; the JobBackgroundService publishes to
/// that group via IHubContext when a job transitions.
///
/// Server-only: clients don't invoke methods. Multi-tab works for free —
/// every active connection for that user is in the same group.
/// </summary>
[Authorize]
public sealed class GenerationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupForUser(userId));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupForUser(userId));

        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupForUser(int userId) => $"user-{userId}";
    public static string GroupForUser(string userId) => $"user-{userId}";
}
