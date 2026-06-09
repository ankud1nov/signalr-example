using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SignalrExample.Models;

namespace SignalrExample.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    // Хардкоженные пользователи для демонстрации — в реальном проекте заменить на БД
    private static readonly Dictionary<string, string> Users = new()
    {
        ["user1"] = "pass1",
        ["user2"] = "pass2",
        ["admin"] = "admin",
    };

    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Выдаёт JWT токен по логину и паролю.
    /// Токен нужно передавать в заголовке Authorization: Bearer {token}
    /// или в query string ?access_token={token} для SignalR WebSocket.
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!Users.TryGetValue(request.Username, out var password) || password != request.Password)
            return Unauthorized(new { error = "Invalid username or password" });

        var token = GenerateJwt(request.Username);
        return Ok(new { token, username = request.Username });
    }

    private string GenerateJwt(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, username),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
