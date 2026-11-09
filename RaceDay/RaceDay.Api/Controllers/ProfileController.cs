using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using RaceDay.Api.Services;
using static RaceDay.Contracts.ResultDto;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuth]   // any logged-in user (Organiser or Participant)
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db)
    {
        _db = db;
    }

    // GET /api/profile/me
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        var userId = HttpContext.Session.GetUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(ToDto(user));
    }

    // PUT /api/profile/me
    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var userId = HttpContext.Session.GetUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var email = request.Email.Trim().ToLower();

        // Another user must not already own this email
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            return Conflict(new { message = "That email is already used by another account." });

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    // POST /api/profile/me/image  (any logged-in user)
    [HttpPost("me/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadImage(
        IFormFile file, [FromServices] BlobStorageService blobStorage)
    {
        var user = await _db.Users.FindAsync(HttpContext.Session.GetUserId());
        if (user == null)
            return NotFound(new { message = "User not found." });

        var problem = BlobStorageService.ValidateImage(file);
        if (problem != null)
            return BadRequest(new { message = problem });

        var url = await blobStorage.UploadAsync(file, "profile-images");
        if (url == null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "The image could not be stored right now. Please try again later." });

        var oldUrl = user.ProfileImageUrl;
        user.ProfileImageUrl = url;
        await _db.SaveChangesAsync();
        await blobStorage.DeleteAsync(oldUrl);

        return Ok(new UploadResponse { ImageUrl = url });
    }

    private static ProfileDto ToDto(AppUser u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        ProfileImageUrl = u.ProfileImageUrl,
        CreatedAt = u.CreatedAt
    };
}