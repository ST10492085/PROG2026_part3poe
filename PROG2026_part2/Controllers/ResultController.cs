using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Captures and shows race results. Organizers record finish times and positions for events they own;
/// everyone can see an event's results; a Participant can see their own results.
/// </summary>
[ApiController]
[Route("api")]
public class ResultController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ResultController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Records a Participant's finish time and finishing position for an event.
    /// Organizer only, and only for events they own. The participant must already be enrolled in the chosen category.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="request">Participant ID, category ID, finish time (hh:mm:ss) and position (1 = first).</param>
    /// <returns>The newly recorded result.</returns>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation failed, category not in this event, or the participant is not enrolled in the category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event or participant does not exist.</response>
    /// <response code="409">A result already exists for this participant and category.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPost("events/{eventId:int}/results")]
    public async Task<ActionResult<ResultResponse>> CreateResult(
        int eventId,
        CreateResultRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // A race cannot be finished in zero or negative time.
        if (request.FinishTime <= TimeSpan.Zero)
        {
            return BadRequest(
                "FinishTime must be greater than zero.");
        }

        // Positions start at 1.
        if (request.Position <= 0)
        {
            return BadRequest(
                "Position must be greater than zero.");
        }

        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized(
                "You must be logged in as an Organizer.");
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // Organizers may only manage results of their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        // The result must belong to an existing Participant account.
        var participant = await _context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == request.UserId &&
                user.Role == "Participant");

        if (participant == null)
        {
            return NotFound(
                "The Participant was not found.");
        }

        // The category must belong to this event.
        var category = await _context.Categories
            .FirstOrDefaultAsync(existingCategory =>
                existingCategory.Id == request.CategoryId &&
                existingCategory.EventId == eventId);

        if (category == null)
        {
            return BadRequest(
                "The selected category does not belong to this event.");
        }

        // Only participants who actually entered this category can receive a result.
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(existingEnrollment =>
                existingEnrollment.UserId == request.UserId &&
                existingEnrollment.EventId == eventId &&
                existingEnrollment.CategoryId == request.CategoryId);

        if (enrollment == null)
        {
            return BadRequest(
                "The Participant is not enrolled in this event category.");
        }

        // At most one result per participant per category.
        bool resultExists = await _context.Results
            .AnyAsync(existingResult =>
                existingResult.UserId == request.UserId &&
                existingResult.EventId == eventId &&
                existingResult.CategoryId == request.CategoryId);

        if (resultExists)
        {
            return Conflict(
                "A result already exists for this Participant and category.");
        }

        // Save the finish time and position together with who/where they belong to.
        var result = new Result
        {
            UserId = request.UserId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            RecordedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);

        await _context.SaveChangesAsync();

        var response = await BuildResultResponse(result.Id);

        return Created(
            $"/api/results/{result.Id}",
            response);
    }

    /// <summary>
    /// Lists all results for an event, ordered by position. Available to everyone (no login needed).
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>The event's results.</returns>
    /// <response code="200">The results were returned (the list may be empty).</response>
    /// <response code="404">The event does not exist.</response>
    [AllowAnonymous]
    [HttpGet("events/{eventId:int}/results")]
    public async Task<ActionResult<List<ResultResponse>>> GetEventResults(
        int eventId)
    {
        bool eventExists = await _context.Events
            .AnyAsync(raceEvent =>
                raceEvent.Id == eventId);

        if (!eventExists)
        {
            return NotFound("Event was not found.");
        }

        var results = await _context.Results
            .Include(result => result.User)
            .Include(result => result.Event)
            .Include(result => result.Category)
            .Where(result => result.EventId == eventId)
            .OrderBy(result => result.Position)
            .ThenBy(result => result.FinishTime)
            .ToListAsync();

        var response = results
            .Select(ToResponse)
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Lists the logged-in Participant's own results, newest first. Participant only.
    /// </summary>
    /// <returns>The participant's results.</returns>
    /// <response code="200">The results were returned (the list may be empty).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organizer.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("results/me")]
    public async Task<ActionResult<List<ResultResponse>>>
        GetMyResults()
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(
                "You must be logged in as a Participant.");
        }

        var results = await _context.Results
            .Include(result => result.User)
            .Include(result => result.Event)
            .Include(result => result.Category)
            .Where(result => result.UserId == userId.Value)
            .OrderByDescending(result => result.RecordedAt)
            .ToListAsync();

        var response = results
            .Select(ToResponse)
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Corrects the finish time and position of an existing result. Organizer only, and only for events they own.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="resultId">The ID of the result to update.</param>
    /// <param name="request">The new finish time (hh:mm:ss) and position.</param>
    /// <returns>The updated result.</returns>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Validation failed (time and position must be greater than zero).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event or result does not exist.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPut("events/{eventId:int}/results/{resultId:int}")]
    public async Task<ActionResult<ResultResponse>> UpdateResult(
        int eventId,
        int resultId,
        UpdateResultRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // A race cannot be finished in zero or negative time.
        if (request.FinishTime <= TimeSpan.Zero)
        {
            return BadRequest(
                "FinishTime must be greater than zero.");
        }

        // Positions start at 1.
        if (request.Position <= 0)
        {
            return BadRequest(
                "Position must be greater than zero.");
        }

        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized(
                "You must be logged in as an Organizer.");
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // Organizers may only manage results of their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        var result = await _context.Results
            .FirstOrDefaultAsync(existingResult =>
                existingResult.Id == resultId &&
                existingResult.EventId == eventId);

        if (result == null)
        {
            return NotFound("Result was not found.");
        }

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;

        await _context.SaveChangesAsync();

        var response = await BuildResultResponse(result.Id);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a result. Organizer only, and only for events they own.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="resultId">The ID of the result to delete.</param>
    /// <returns>No content when the result was deleted.</returns>
    /// <response code="204">Result deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event or result does not exist.</response>
    [Authorize(Roles = "Organizer")]
    [HttpDelete("events/{eventId:int}/results/{resultId:int}")]
    public async Task<IActionResult> DeleteResult(
        int eventId,
        int resultId)
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized(
                "You must be logged in as an Organizer.");
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // Organizers may only manage results of their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        var result = await _context.Results
            .FirstOrDefaultAsync(existingResult =>
                existingResult.Id == resultId &&
                existingResult.EventId == eventId);

        if (result == null)
        {
            return NotFound("Result was not found.");
        }

        _context.Results.Remove(result);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Reads the logged-in user's ID from the session claims (created at login).
    /// Returns null when the claim is missing or is not a number.
    /// </summary>
    private int? GetCurrentUserId()
    {
        string? userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdValue, out int userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Loads a result with participant, event and category names for the response body.
    /// </summary>
    private async Task<ResultResponse?> BuildResultResponse(
        int resultId)
    {
        var result = await _context.Results
            .Include(existingResult => existingResult.User)
            .Include(existingResult => existingResult.Event)
            .Include(existingResult => existingResult.Category)
            .FirstOrDefaultAsync(existingResult =>
                existingResult.Id == resultId);

        return result == null
            ? null
            : ToResponse(result);
    }

    /// <summary>
    /// Maps a Result entity to the DTO returned to API callers.
    /// </summary>
    private static ResultResponse ToResponse(Result result)
    {
        return new ResultResponse
        {
            Id = result.Id,
            UserId = result.UserId,
            EventId = result.EventId,
            CategoryId = result.CategoryId,
            ParticipantName = result.User == null
                ? string.Empty
                : $"{result.User.FirstName} {result.User.LastName}",
            EventName = result.Event == null
                ? string.Empty
                : result.Event.Name,
            CategoryName = result.Category == null
                ? string.Empty
                : result.Category.Name,
            FinishTime = result.FinishTime,
            Position = result.Position,
            RecordedAt = result.RecordedAt
        };
    }
}