using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Data;
using NcaaBracket.Api.DTOs;
using NcaaBracket.Api.Models;
using NcaaBracket.Api.Services;

namespace NcaaBracket.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GoogleAuthService _googleAuth;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext db, GoogleAuthService googleAuth, JwtService jwtService)
    {
        _db = db;
        _googleAuth = googleAuth;
        _jwtService = jwtService;
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var payload = await _googleAuth.ValidateTokenAsync(request.IdToken);
        if (payload is null)
            return Unauthorized(new { message = "Invalid Google token" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

        if (user is null)
        {
            var firstName = payload.GivenName ?? payload.Name ?? payload.Email;
            user = new User
            {
                Id = Guid.NewGuid(),
                GoogleId = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.Name ?? payload.Email,
                AvatarUrl = payload.Picture,
                BracketTitle = $"{firstName}'s Bracket",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            user.Email = payload.Email;
            user.DisplayName = payload.Name ?? payload.Email;
            user.AvatarUrl = payload.Picture;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                BracketTitle = user.BracketTitle,
                Role = user.Role
            }
        });
    }
}
