using SignalrExample.Models;

namespace SignalrExample.Hubs;

/// <summary>
/// Интерфейс описывает методы, которые сервер вызывает на клиенте.
/// Используется в TypedChatHub для строгой типизации вместо строковых SendAsync.
/// </summary>
public interface IChatClient
{
    Task ReceiveMessage(ChatMessage message);
    Task UserConnected(string connectionId, string user);
    Task UserDisconnected(string connectionId, string user);
}
