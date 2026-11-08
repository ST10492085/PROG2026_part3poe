using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;
using PROG2026_part2.Services;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Manages race events. Everyone can view events; only Organizers can create events,
/// and an Organizer can only change or delete the events they own.
/// </summary>
[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public EventController(
        ApplicationDbContext context,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Lists all RaceDay events (with their categories), ordered by date. Available to everyone (no login needed).
    /// </summary>
    /// <returns>A list of events.</returns>
    /// <response code="200">The events were returned (the list may be empty).</response>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<EventResponse>>> GetAllEvents()
    {
        var events = await _context.Events
            .Include(raceEvent => raceEvent.Categories)
            .OrderBy(raceEvent => raceEvent.EventDate)
            .ToListAsync();

        var response = events
            .Select(ToEventResponse)
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Returns one event, including its categories. Available to everyone (no login needed).
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>The requested event.</returns>
    /// <response code="200">The event was returned.</response>
    /// <response code="404">The event does not exist.</response>
    [AllowAnonymous]
    [HttpGet("{eventId:int}")]
    public async Task<ActionResult<EventResponse>> GetEventById(
        int eventId)
    {
        var raceEvent = await _context.Events
            .Include(existingEvent => existingEvent.Categories)
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        return Ok(ToEventResponse(raceEvent));
    }

    /// <summary>
    /// Creates an event owned by the logged-in Organizer. Organizer only.
    /// </summary>
    /// <param name="request">Name, description, date, location, distance in km and event type (Run, Walk or Cycle).</param>
    /// <returns>The newly created event.</returns>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed, or EventType is not Run, Walk or Cycle.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPost]
    public async Task<ActionResult<EventResponse>> CreateEvent(
        CreateEventRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Event type is limited to the three values in the brief: run, walk or cycle.
        if (!IsValidEventType(request.EventType))
        {
            return BadRequest("EventType must be Run, Walk, or Cycle.");
        }

        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized("You must be logged in as an Organizer.");
        }

        // The Organizer is taken from the session so nobody can create events for someone else.
        var raceEvent = new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = NormaliseEventType(request.EventType),
            OrganizerId = organizerId.Value
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        // 201 Created + a Location header that points to the new event.
        return CreatedAtAction(
            nameof(GetEventById),
            new { eventId = raceEvent.Id },
            ToEventResponse(raceEvent));
    }

    /// <summary>
    /// Updates an event. Only the fields supplied in the request are changed. Organizer only, and only for their own events.
    /// </summary>
    /// <param name="eventId">The ID of the event to update.</param>
    /// <param name="request">Any of: name, description, date, location, distance, event type.</param>
    /// <returns>The updated event.</returns>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed, or EventType is not Run, Walk or Cycle.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event does not exist.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPut("{eventId:int}")]
    public async Task<ActionResult<EventResponse>> UpdateEvent(
        int eventId,
        UpdateEventRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var raceEvent = await _context.Events
            .Include(existingEvent => existingEvent.Categories)
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized();
        }

        // Organizers may only change or delete their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        // Partial update: a field is only changed when a value was supplied for it.
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            raceEvent.Name = request.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            raceEvent.Description = request.Description.Trim();
        }

        if (request.EventDate.HasValue)
        {
            raceEvent.EventDate = request.EventDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            raceEvent.Location = request.Location.Trim();
        }

        if (request.DistanceKm.HasValue)
        {
            raceEvent.DistanceKm = request.DistanceKm.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            if (!IsValidEventType(request.EventType))
            {
                return BadRequest(
                    "EventType must be Run, Walk, or Cycle.");
            }

            raceEvent.EventType =
                NormaliseEventType(request.EventType);
        }

        await _context.SaveChangesAsync();

        return Ok(ToEventResponse(raceEvent));
    }

    /// <summary>
    /// Deletes an event and its categories. Organizer only, and only for their own events.
    /// An event that already has enrolments or results cannot be deleted.
    /// </summary>
    /// <param name="eventId">The ID of the event to delete.</param>
    /// <returns>No content when the event was deleted.</returns>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">The event has enrolments or results.</response>
    [Authorize(Roles = "Organizer")]
    [HttpDelete("{eventId:int}")]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized();
        }

        // Organizers may only change or delete their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        // Enrollments and Results are linked with Restrict foreign keys, so the database refuses to
        // delete an event that still has them. Check first and answer with a clear 409 instead of a 500.
        bool hasEnrollments = await _context.Enrollments
            .AnyAsync(enrollment => enrollment.EventId == eventId);

        bool hasResults = await _context.Results
            .AnyAsync(result => result.EventId == eventId);

        if (hasEnrollments || hasResults)
        {
            return Conflict(
                "This event cannot be deleted because it has enrollments or results.");
        }

        string? bannerUrl = raceEvent.BannerImageUrl;

        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync();

        // Tidy up: the banner image is no longer needed in Azure Blob Storage.
        await _blobStorage.DeleteImageAsync(bannerUrl);

        return NoContent();
    }

    /// <summary>
    /// Uploads (or replaces) the banner image of an event. The image is stored in Azure Blob Storage
    /// and its URL is saved on the event. Organizer only, and only for events they own.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="file">The image file (form field "file"): JPG, PNG, GIF or WEBP, maximum 5 MB.</param>
    /// <returns>The updated event, including the new BannerImageUrl.</returns>
    /// <response code="200">Banner uploaded and the event updated.</response>
    /// <response code="400">No file, file too large, or not an allowed image type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="503">Azure Blob Storage is not configured on the server.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPost("{eventId:int}/banner")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EventResponse>> UploadBanner(int eventId, IFormFile file)
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized();
        }

        var raceEvent = await _context.Events
            .Include(existingEvent => existingEvent.Categories)
            .FirstOrDefaultAsync(existingEvent => existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // Organizers may only change their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        // Check size and type before talking to Azure.
        string? validationError = ImageUploadValidator.Validate(file);

        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        if (!_blobStorage.IsConfigured)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Azure Blob Storage is not configured on the server.");
        }

        // 1) Upload the new image, 2) save its URL in the database, 3) delete the old image.
        string? oldBannerUrl = raceEvent.BannerImageUrl;

        raceEvent.BannerImageUrl = await _blobStorage.UploadImageAsync(file, "events");
        await _context.SaveChangesAsync();

        await _blobStorage.DeleteImageAsync(oldBannerUrl);

        return Ok(ToEventResponse(raceEvent));
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
    /// An event type is valid when it is Run, Walk or Cycle (any letter case).
    /// </summary>
    private static bool IsValidEventType(string eventType)
    {
        return eventType.Equals(
                   "Run",
                   StringComparison.OrdinalIgnoreCase)
            || eventType.Equals(
                   "Walk",
                   StringComparison.OrdinalIgnoreCase)
            || eventType.Equals(
                   "Cycle",
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stores event types consistently, e.g. "run" becomes "Run".
    /// </summary>
    private static string NormaliseEventType(string eventType)
    {
        return char.ToUpper(eventType.Trim()[0])
            + eventType.Trim()[1..].ToLower();
    }

    /// <summary>
    /// Maps an Event entity (and its categories) to the DTO returned to API callers.
    /// </summary>
    private static EventResponse ToEventResponse(Event raceEvent)
    {
        return new EventResponse
        {
            Id = raceEvent.Id,
            Name = raceEvent.Name,
            Description = raceEvent.Description,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            DistanceKm = raceEvent.DistanceKm,
            EventType = raceEvent.EventType,
            OrganizerId = raceEvent.OrganizerId,
            BannerImageUrl = raceEvent.BannerImageUrl,
            Categories = raceEvent.Categories
                .Select(category => new CategoryResponse
                {
                    Id = category.Id,
                    EventId = category.EventId,
                    Name = category.Name,
                    Description = category.Description,
                    MinAge = category.MinAge,
                    MaxAge = category.MaxAge,
                    DistanceKm = category.DistanceKm
                })
                .ToList()
        };
    }
}