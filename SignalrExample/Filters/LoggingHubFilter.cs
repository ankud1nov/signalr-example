using Microsoft.AspNetCore.SignalR;

namespace SignalrExample.Filters;

/// <summary>
/// Логирует каждый вызов метода хаба: имя метода, пользователя и время выполнения.
/// </summary>
public class LoggingHubFilter : IHubFilter
{
    private readonly ILogger<LoggingHubFilter> _logger;

    public LoggingHubFilter(ILogger<LoggingHubFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var hubName = invocationContext.Hub.GetType().Name;
        var methodName = invocationContext.HubMethodName;
        var user = invocationContext.Context.User?.Identity?.Name ?? invocationContext.Context.ConnectionId;

        _logger.LogInformation("{Hub}.{Method} called by {User}", hubName, methodName, user);

        var start = DateTime.UtcNow;
        try
        {
            var result = await next(invocationContext);
            var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
            _logger.LogInformation("{Hub}.{Method} completed in {Elapsed}ms", hubName, methodName, elapsed);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Hub}.{Method} threw an exception", hubName, methodName);
            throw;
        }
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        _logger.LogInformation(
            "Connected. Hub: {HubName}, ConnectionId: {ConnectionId}, User: {User}",
            context.Hub.GetType().Name,
            context.Context.ConnectionId,
            GetUserName(context));

        return next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        _logger.LogInformation(
            "Disconnected. Hub: {HubName}, ConnectionId: {ConnectionId}, User: {User}",
            context.Hub.GetType().Name,
            context.Context.ConnectionId,
            GetUserName(context));
        return next(context, exception);
    }
    
    /// <summary>
    /// Наверное стоит сделать при авторизации установку pluginName в Context.User.Identity.Name
    /// </summary>
    private string GetUserName(HubLifetimeContext context) => context.Context.User?.Identity?.Name ?? context.Context.ConnectionId;
}
