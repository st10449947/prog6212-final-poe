using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
[SessionAuth]   // every endpoint needs a login; each one then sets its own role
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // POST /api/results  (Organiser who owns the event)
    [HttpPost]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        // The Organiser must own the event this enrolment belongs to
        var organiserId = HttpContext.Session.GetUserId();
        if (enrolment.Event!.UserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only record results for events you created." });

        if (enrolment.Status == "Cancelled")
            return BadRequest(new { message = "A cancelled enrolment cannot have a result." });

        // One result per enrolment
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict(new { message = "A result has already been recorded for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            UserId = organiserId,                       // the Organiser who captured it
            FinishTimeSeconds = request.FinishTimeSeconds,
            FinishPosition = request.FinishPosition
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        result.Enrolment = enrolment;
        return StatusCode(StatusCodes.Status201Created, ToDto(result));
    }

    // GET /api/results/mine  (Participant only: their own results)
    [HttpGet("mine")]
    [SessionAuth("Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetUserId();

        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Where(r => r.Enrolment!.UserId == userId)
            .OrderByDescending(r => r.Enrolment!.Event!.EventDate)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    // GET /api/results/event/5  (Organiser who owns the event)
    [HttpGet("event/{eventId:int}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
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
                new { message = "You can only view results for events you created." });

        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.FinishPosition)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    private static ResultDto ToDto(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventName = r.Enrolment?.Event?.EventName ?? string.Empty,
        EventDate = r.Enrolment?.Event?.EventDate ?? default,
        CategoryName = r.Enrolment?.Category?.CategoryName ?? string.Empty,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? string.Empty,
        FinishTimeSeconds = r.FinishTimeSeconds,
        FinishPosition = r.FinishPosition,
        RecordedAt = r.RecordedAt
    };
}