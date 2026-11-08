using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Lets an Organizer view (and, if needed, remove) the enrolments for events they own.
/// Participants receive 403 Forbidden.
/// </summary>
[ApiController]
[Authorize(Roles = "Organizer")]
[Route("api/organizer/events/{eventId:int}/enrollments")]
public class OrganizerEnrollmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganizerEnrollmentController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all enrolments (participant, category and enrolment date) for an event the Organizer owns.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>The event's enrolments, oldest first.</returns>
    /// <response code="200">The enrolments were returned (the list may be empty).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet]
    public async Task<
        ActionResult<List<OrganizerEnrollmentResponse>>>
        GetEventEnrollments(int eventId)
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

        // An Organizer may only see/change enrolments of their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        var enrollments = await _context.Enrollments
            .Include(enrollment => enrollment.User)
            .Include(enrollment => enrollment.Event)
            .Include(enrollment => enrollment.Category)
            .Where(enrollment => enrollment.EventId == eventId)
            .OrderBy(enrollment => enrollment.EnrolledAt)
            .ToListAsync();

        var response = enrollments
            .Select(ToResponse)
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Removes an enrolment from an event the Organizer owns. Not possible once a result has been recorded for it.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="enrollmentId">The ID of the enrolment to remove.</param>
    /// <returns>No content when the enrolment was removed.</returns>
    /// <response code="204">Enrolment removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event or enrolment does not exist.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpDelete("{enrollmentId:int}")]
    public async Task<IActionResult> RemoveEnrollment(
        int eventId,
        int enrollmentId)
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

        // An Organizer may only see/change enrolments of their own events.
        if (raceEvent.OrganizerId != organizerId.Value)
        {
            return Forbid();
        }

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(existingEnrollment =>
                existingEnrollment.Id == enrollmentId &&
                existingEnrollment.EventId == eventId);

        if (enrollment == null)
        {
            return NotFound("Enrollment was not found.");
        }

        // An enrolment that already has a result is part of the official record.
        bool resultExists = await _context.Results
            .AnyAsync(result =>
                result.UserId == enrollment.UserId &&
                result.EventId == enrollment.EventId &&
                result.CategoryId == enrollment.CategoryId);

        if (resultExists)
        {
            return Conflict(
                "This enrollment cannot be removed because a result has been recorded.");
        }

        _context.Enrollments.Remove(enrollment);

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
    /// Maps an Enrollment (with participant, event and category loaded) to the Organizer-facing DTO.
    /// </summary>
    private static OrganizerEnrollmentResponse ToResponse(
        Enrollment enrollment)
    {
        return new OrganizerEnrollmentResponse
        {
            Id = enrollment.Id,
            UserId = enrollment.UserId,
            ParticipantName = enrollment.User == null
                ? string.Empty
                : $"{enrollment.User.FirstName} {enrollment.User.LastName}",
            ParticipantEmail = enrollment.User?.Email
                ?? string.Empty,
            EventId = enrollment.EventId,
            EventName = enrollment.Event?.Name
                ?? string.Empty,
            CategoryId = enrollment.CategoryId,
            CategoryName = enrollment.Category?.Name
                ?? string.Empty,
            EnrolledAt = enrollment.EnrolledAt
        };
    }
}