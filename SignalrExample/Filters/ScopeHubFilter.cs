using Microsoft.AspNetCore.SignalR;

namespace SignalrExample.Filters;

/// <summary>
/// Создаёт отдельный DI-скоуп для каждого вызова метода хаба.
/// Это нужно когда хаб зарегистрирован как Singleton, но методу нужны Scoped-сервисы (например, DbContext).
/// </summary>
public class ScopeHubFilter : IHubFilter
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopeHubFilter(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Хабы по умолчанию Transient, поэтому скоуп уже есть.
        // Этот фильтр демонстрирует паттерн для случаев, когда нужен явный скоуп.
        await using var scope = _scopeFactory.CreateAsyncScope();

        // Scoped-сервисы доступны через scope.ServiceProvider
        // Например: var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await next(invocationContext);
    }
}
