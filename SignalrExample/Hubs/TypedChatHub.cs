using Microsoft.AspNetCore.SignalR;
using SignalrExample.Models;

namespace SignalrExample.Hubs;

/// <summary>
/// Типизированный Hub: клиентские методы вызываются через IChatClient без строковых имён.
/// Доступен анонимно — для демонстрации разницы с ChatHub.
/// </summary>
public class TypedChatHub : Hub<IChatClient>
{
    private readonly ILogger<TypedChatHub> _logger;

    public TypedChatHub(ILogger<TypedChatHub> logger)
    {
        _logger = logger;
    }

    // --- Отправка сообщений ---

    public async Task SendToAll(string message)
    {
        var msg = new ChatMessage(GetUserName(), message, null, DateTime.UtcNow);
        await Clients.All.ReceiveMessage(msg);
    }

    public async Task SendToUser(string connectionId, string message)
    {
        var msg = new ChatMessage(GetUserName(), message, null, DateTime.UtcNow);
        await Clients.Client(connectionId).ReceiveMessage(msg);
    }

    public async Task SendToGroup(string group, string message)
    {
        var msg = new ChatMessage(GetUserName(), message, group, DateTime.UtcNow);
        await Clients.Group(group).ReceiveMessage(msg);
    }

    // --- Управление группами ---

    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        // Уведомляем группу — используем ReceiveMessage как универсальное событие
        var notice = new ChatMessage("System", $"{GetUserName()} joined group '{group}'", group, DateTime.UtcNow);
        await Clients.Group(group).ReceiveMessage(notice);
    }

    public async Task LeaveGroup(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        var notice = new ChatMessage("System", $"{GetUserName()} left group '{group}'", group, DateTime.UtcNow);
        await Clients.Group(group).ReceiveMessage(notice);
    }

    // --- События подключения ---

    public override async Task OnConnectedAsync()
    {
        await Clients.Others.UserConnected(Context.ConnectionId, GetUserName());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.Others.UserDisconnected(Context.ConnectionId, GetUserName());
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserName() => Context.User?.Identity?.Name ?? Context.ConnectionId;
}
