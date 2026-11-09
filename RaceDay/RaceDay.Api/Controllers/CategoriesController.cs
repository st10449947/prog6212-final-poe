using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[SessionAuth]   // every endpoint needs a login; write endpoints also need the Organiser role
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    // GET /api/events/5/categories  (both roles)
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories.Select(ToDto));
    }

    // POST /api/events/5/categories  (Organiser who owns the event)
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CreateCategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to events you created." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(category));
    }

    // PUT /api/categories/3  (Organiser who owns the parent event)
    [HttpPut("categories/{id:int}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CreateCategoryRequest request)
    {
        // Include the parent event so we can check who owns it
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit categories of events you created." });

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;

        await _db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    // DELETE /api/categories/3  (Organiser who owns the parent event)
    [HttpDelete("categories/{id:int}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of events you created." });

        // Enrolments are protected (Restrict), so refuse cleanly instead of crashing
        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return Conflict(new { message = "This category already has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Category deleted." });
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants
    };
}