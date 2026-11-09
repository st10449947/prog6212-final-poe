using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private static readonly string[] ValidRoles = { "Organiser", "Participant" };

    private readonly RaceDayDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    // POST /api/auth/register
    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ValidRoles.Contains(request.Role))
            return BadRequest(new { message = "Role must be Organiser or Participant." });

        var email = request.Email.Trim().ToLower();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "That email is already registered." });

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);   // never store the plain password

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }

   
    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so emails can't be probed
        if (user == null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(ToResponse(user));
    }

    // POST /api/auth/logout
    
    [HttpPost("logout")]
    [SessionAuth]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    private static AuthResponse ToResponse(AppUser u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role
    };
}