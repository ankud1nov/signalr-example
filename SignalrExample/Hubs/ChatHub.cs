using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalrExample.Models;

namespace SignalrExample.Hubs;

/// <summary>
/// Базовый Hub с нетипизированными клиентскими вызовами через строковые имена методов.
/// Требует авторизации (JWT токен).
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    // --- Отправка сообщений ---

    /// <summary>Рассылает сообщение всем подключённым клиентам.</summary>
    public async Task SendToAll(string message)
    {
        var msg = new ChatMessage(GetUserName(), message, null, DateTime.UtcNow);
        await Clients.All.SendAsync("ReceiveMessage", msg);
    }

    /// <summary>Отправляет сообщение конкретному пользователю по его connectionId.</summary>
    public async Task SendToUser(string connectionId, string message)
    {
        var msg = new ChatMessage(GetUserName(), message, null, DateTime.UtcNow);
        await Clients.Client(connectionId).SendAsync("ReceiveMessage", msg);
    }

    /// <summary>Отправляет сообщение всем участникам указанной группы.</summary>
    public async Task SendToGroup(string group, string message)
    {
        var msg = new ChatMessage(GetUserName(), message, group, DateTime.UtcNow);
        await Clients.Group(group).SendAsync("ReceiveMessage", msg);
    }

    // --- Управление группами ---

    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Group(group).SendAsync("UserJoinedGroup", Context.ConnectionId, GetUserName(), group);
    }

    public async Task LeaveGroup(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        await Clients.Group(group).SendAsync("UserLeftGroup", Context.ConnectionId, GetUserName(), group);
    }

    // --- События подключения ---

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}, User: {User}", Context.ConnectionId, GetUserName());
        await Clients.Others.SendAsync("UserConnected", Context.ConnectionId, GetUserName());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}, User: {User}", Context.ConnectionId, GetUserName());
        await Clients.Others.SendAsync("UserDisconnected", Context.ConnectionId, GetUserName());
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserName() => Context.User?.Identity?.Name ?? Context.ConnectionId;
}
