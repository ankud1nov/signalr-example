# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core 8.0 Web API проект-заготовка для демонстрации SignalR. Пока содержит только стандартный scaffolded контроллер `WeatherForecast`; SignalR ещё не подключён.

## Commands

```bash
# Сборка
dotnet build SignalrExample/SignalrExample.csproj

# Запуск (http://localhost:5555, Swagger UI на /swagger)
dotnet run --project SignalrExample/SignalrExample.csproj

# Запуск через профиль
dotnet run --project SignalrExample/SignalrExample.csproj --launch-profile http

# Docker
docker compose up --build
```

Тестовых проектов нет — добавить при необходимости.

## Architecture

Единственный проект `SignalrExample` (net8.0, Nullable enabled, ImplicitUsings enabled):

- `Program.cs` — точка входа; настройка DI-контейнера и middleware pipeline (Controllers, Swagger, Authorization).
- `Controllers/` — стандартные MVC-контроллеры (`[ApiController]`, route `[controller]`).
- `WeatherForecast.cs` — модель-заглушка из шаблона.

Swagger UI доступен только в `Development` (`ASPNETCORE_ENVIRONMENT=Development`).

Docker-образ строится многоэтапно (`Dockerfile`): base → build → publish → final; `compose.yaml` поднимает один сервис `signalrexample`.

## Adding SignalR

Для добавления SignalR:
1. `builder.Services.AddSignalR()` в `Program.cs`.
2. Создать Hub-класс, наследующий `Microsoft.AspNetCore.SignalR.Hub`.
3. `app.MapHub<MyHub>("/hub-path")` перед `app.Run()`.
