# SignalR Demo

Демонстрационный проект ASP.NET Core 8.0, покрывающий основные возможности SignalR.

## Что реализовано

| Компонент | Описание |
|---|---|
| `ChatHub` | Авторизованный хаб (JWT). Broadcast, личное сообщение, группы. |
| `TypedChatHub` | Анонимный хаб с типизированными клиентскими методами (`IChatClient`). |
| `LoggingHubFilter` | Логирует имя метода, пользователя и время выполнения. |
| `ScopeHubFilter` | Создаёт DI-скоуп на каждый вызов метода. |
| `AuthController` | `POST /auth/login` → JWT токен (HS256, 1 час). |
| `index.html` | Telegram-like SPA: группы, broadcast, личные сообщения, список онлайн. |

## Пользователи для теста

| Логин | Пароль |
|---|---|
| user1 | pass1 |
| user2 | pass2 |
| admin | admin |

## Запуск

### Локально

```bash
dotnet run --project SignalrExample/SignalrExample.csproj
```

Открыть: http://localhost:5555

### Docker Compose

```bash
docker compose up --build
```

Приложение будет доступно на порту, указанном в `compose.yaml` (по умолчанию — 8080 внутри контейнера).

> **Swagger UI:** http://localhost:5555/swagger (только в `Development`)

## Хабы

| Хаб | Маршрут | Авторизация |
|---|---|---|
| `ChatHub` | `/hubs/chat` | Требует JWT |
| `TypedChatHub` | `/hubs/typed-chat` | Анонимный |

### Передача JWT токена

SignalR не может передать `Authorization` заголовок при WebSocket-подключении.  
Токен передаётся в query string:

```
/hubs/chat?access_token=<jwt>
```

Это настроено в `Program.cs` через `JwtBearerEvents.OnMessageReceived`.

### Методы хабов

```
SendToAll(message)               — всем клиентам
SendToUser(connectionId, message) — конкретному клиенту
SendToGroup(group, message)       — всей группе
JoinGroup(group)                  — войти в группу
LeaveGroup(group)                 — выйти из группы
```

### Клиентские события (слушать на клиенте)

```
ReceiveMessage(ChatMessage)       — входящее сообщение
UserConnected(connectionId, user) — клиент подключился
UserDisconnected(connectionId, user) — клиент отключился
UserJoinedGroup / UserLeftGroup   — членство в группе (ChatHub)
```

## Авторизация

`POST /auth/login` возвращает JWT токен:

```json
{ "username": "user1", "password": "pass1" }
```

Ответ:

```json
{ "token": "eyJ...", "username": "user1" }
```

Токен использовать в заголовке `Authorization: Bearer <token>` для REST, или в query string для SignalR.

## Горизонтальное масштабирование

При нескольких инстансах приложения SignalR-сообщения не пересекают границы процессов. Нужен **backplane** — общая шина для всех инстансов.

Решение: **Redis backplane** через `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.

```bash
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379");
```

После этого все инстансы будут обмениваться сообщениями через Redis.  
`compose.yaml` при необходимости нужно дополнить сервисом `redis`.

## Структура проекта

```
SignalrExample/
  Controllers/
    AuthController.cs       — POST /auth/login
  Filters/
    LoggingHubFilter.cs     — логирование вызовов
    ScopeHubFilter.cs       — DI-скоуп на вызов
  Hubs/
    ChatHub.cs              — Hub (авторизованный)
    TypedChatHub.cs         — Hub<IChatClient> (анонимный)
    IChatClient.cs          — интерфейс клиентских методов
  Models/
    ChatMessage.cs          — DTO сообщения
    LoginRequest.cs         — DTO для логина
  wwwroot/
    index.html              — Telegram-like UI
  Program.cs                — DI, middleware, маршруты хабов
  appsettings.json          — JWT-настройки
```
