using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
[SessionAuth]   // every endpoint needs a login; each one then sets its own role
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // POST /api/enrolments  (Participant only)
    [HttpPost]
    [SessionAuth("Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateEnrolmentRequest request)
    {
        var userId = HttpContext.Session.GetUserId();

        var ev = await _db.Events.FindAsync(request.EventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var category = await _db.Categories.FindAsync(request.CategoryId);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        // The chosen category must belong to the chosen event
        if (category.EventId != request.EventId)
            return BadRequest(new { message = "That category does not belong to this event." });

        // A Participant can enter each event only once
        if (await _db.Enrolments.AnyAsync(e => e.UserId == userId && e.EventId == request.EventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        // Respect the category's maximum (cancelled enrolments free up a place)
        var taken = await _db.Enrolments.CountAsync(e =>
            e.CategoryId == category.CategoryId && e.Status != "Cancelled");
        if (taken >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment
        {
            UserId = userId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            Status = "Confirmed"
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        // Attach the related rows so the response can show names
        enrolment.Event = ev;
        enrolment.Category = category;
        enrolment.Participant = await _db.Users.FindAsync(userId);

        return StatusCode(StatusCodes.Status201Created, ToDto(enrolment));
    }

    // GET /api/enrolments/mine  (Participant only: their own enrolments)
    [HttpGet("mine")]
    [SessionAuth("Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetUserId();

        var enrolments = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Event!.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    // GET /api/enrolments/event/5  (Organiser who owns the event)
    [HttpGet("event/{eventId:int}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for events you created." });

        var enrolments = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    // PUT /api/enrolments/3/status  (Organiser who owns the event)
    [HttpPut("{id:int}/status")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event!.UserId != HttpContext.Session.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only manage enrolments for events you created." });

        enrolment.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(ToDto(enrolment));
    }

    private static EnrolmentDto ToDto(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        EventId = e.EventId,
        EventName = e.Event?.EventName ?? string.Empty,
        EventDate = e.Event?.EventDate ?? default,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.CategoryName ?? string.Empty,
        UserId = e.UserId,
        ParticipantName = e.Participant?.FullName ?? string.Empty,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };
}