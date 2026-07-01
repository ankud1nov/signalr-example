using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SignalR.OpenApi.Extensions;
using SignalrExample.Filters;
using SignalrExample.Hubs;

namespace SignalrExample;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = $"JWT Authorization header using the Bearer scheme. {Environment.NewLine}" +
                               "Example: '12345abcdef'",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>()
                }
            });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);   
            }
        });

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
        builder.Services.AddSignalROpenApi(options =>
        {
            // User-enterable headers shown in the SwaggerUI Authorize dialog.
            // Each entry appears as an apiKey security scheme (in: header) so users
            // can enter a value at runtime before invoking hub methods.
            options.ApiKeyHeaders["X-Custom-Header"] = "A custom header sent with every hub connection.";

            // Security schemes applied to operations with [Authorize].
            // Define the authentication methods that SwaggerUI exposes in the Authorize dialog.
            options.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Bearer token for SignalR hub authentication.",
            };
        });
        builder.Services.AddSignalRSwaggerUi();

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
        app.MapSignalROpenApi();
        app.UseSignalRSwaggerUi();

        app.Run();
    }
}
