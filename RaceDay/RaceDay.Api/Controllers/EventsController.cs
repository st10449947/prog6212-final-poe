using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using static RaceDay.Contracts.ResultDto;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[SessionAuth]   // every endpoint needs a login; some also need the Organiser role
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // GET /api/events  (both roles)
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .Include(e => e.Organiser)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToDto));
    }

    // GET /api/events/5  (both roles)
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        return Ok(ToDto(ev));
    }

    // GET /api/events/mine  (Organiser only: just their own events)
    [HttpGet("mine")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetUserId();

        var events = await _db.Events
            .Include(e => e.Organiser)
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToDto));
    }

    // POST /api/events  (Organiser only)
    [HttpPost]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
  
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        var ev = new Event
        {
            UserId = HttpContext.Session.GetUserId(),   // the logged-in Organiser owns it
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = request.EventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDto(ev));
    }

    // PUT /api/events/5  (Organiser who owns it)
    [HttpPut("{id:int}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> Update(int id, UpdateEventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit events you created." });

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    // DELETE /api/events/5  (Organiser who owns it)
    [HttpDelete("{id:int}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete events you created." });

        // Enrolments are protected (Restrict), so refuse cleanly instead of crashing
        if (await _db.Enrolments.AnyAsync(en => en.EventId == id))
            return Conflict(new { message = "This event already has enrolments and cannot be deleted." });

        _db.Events.Remove(ev);   // its categories are removed automatically (cascade)
        await _db.SaveChangesAsync();

        return Ok(new { message = "Event deleted." });
    }

    // POST /api/events/5/banner  (Organiser who owns the event)
    [HttpPost("{id:int}/banner")]
    [SessionAuth("Organiser")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(
        int id, IFormFile file, [FromServices] BlobStorageService blobStorage)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        // Same ownership rule as editing the event
        if (ev.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only upload banners for events you created." });

        var problem = BlobStorageService.ValidateImage(file);
        if (problem != null)
            return BadRequest(new { message = problem });

        var url = await blobStorage.UploadAsync(file, "event-banners");
        if (url == null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "The image could not be stored right now. Please try again later." });

        var oldUrl = ev.BannerImageUrl;
        ev.BannerImageUrl = url;                 // the URL is what we keep in SQL Server
        await _db.SaveChangesAsync();
        await blobStorage.DeleteAsync(oldUrl);   // tidy up the replaced banner

        return Ok(new UploadResponse { ImageUrl = url });
    }

    private static EventDto ToDto(Event e) => new()
    {
        EventId = e.EventId,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        BannerImageUrl = e.BannerImageUrl,
        OrganiserId = e.UserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty
    };
}