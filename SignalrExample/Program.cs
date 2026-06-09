using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using SignalrExample.Filters;
using SignalrExample.Hubs;

namespace SignalrExample;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // --- SignalR ---
        builder.Services.AddSignalR(options =>
            {
                options.AddFilter<ScopeHubFilter>();
                options.AddFilter<LoggingHubFilter>();
            })
            .AddJsonProtocol(options =>
            {
                // camelCase для совместимости с JS-клиентом
                options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                // null-поля не передаём по сети
                options.PayloadSerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

        // Фильтры регистрируем как сервисы, чтобы DI мог их создать
        builder.Services.AddScoped<LoggingHubFilter>();
        builder.Services.AddScoped<ScopeHubFilter>();

        // --- CORS ---
        // В продакшене заменить на конкретные origins
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        // --- JWT авторизация ---
        var jwtKey = builder.Configuration["Jwt:Key"]!;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                };

                // SignalR передаёт токен в query string для WebSocket и SSE транспортов
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseDefaultFiles();   // отдаёт index.html по запросу к /
        app.UseStaticFiles();    // отдаёт файлы из wwwroot/

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat");
        app.MapHub<TypedChatHub>("/hubs/typed-chat");

        app.Run();
    }
}
