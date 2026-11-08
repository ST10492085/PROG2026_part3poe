using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Lets Participants enter events by choosing a category, view their own enrolments and cancel them.
/// Every endpoint here requires a logged-in Participant; Organizers receive 403 Forbidden.
/// </summary>
[ApiController]
[Authorize(Roles = "Participant")]
public class EnrollmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnrollmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enrols the logged-in Participant in an event by selecting one of the event's categories.
    /// The link between participant, event and category is saved. A participant can enter an event only once.
    /// </summary>
    /// <param name="eventId">The ID of the event to enter.</param>
    /// <param name="request">The ID of the chosen category (must belong to this event).</param>
    /// <returns>The new enrolment, including event and category names.</returns>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Validation failed, or the category does not belong to this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organizer.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">The participant is already enrolled in this event.</response>
    [HttpPost("api/events/{eventId:int}/enrollments")]
    public async Task<ActionResult<EnrollmentResponse>> CreateEnrollment(
        int eventId,
        CreateEnrollmentRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized("You must be logged in as a Participant.");
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // The chosen category must belong to the event being entered.
        var category = await _context.Categories
            .FirstOrDefaultAsync(existingCategory =>
                existingCategory.Id == request.CategoryId &&
                existingCategory.EventId == eventId);

        if (category == null)
        {
            return BadRequest(
                "The selected category does not belong to this event.");
        }

        // One enrolment per participant per event (also enforced by a unique database index).
        bool alreadyEnrolled = await _context.Enrollments
            .AnyAsync(enrollment =>
                enrollment.UserId == userId.Value &&
                enrollment.EventId == eventId);

        if (alreadyEnrolled)
        {
            return Conflict(
                "You are already enrolled in this event.");
        }

        // Record the link between Participant, Event and Category.
        var enrollment = new Enrollment
        {
            UserId = userId.Value,
            EventId = eventId,
            CategoryId = category.Id,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var response = await BuildResponse(enrollment.Id);

        return Created(
            $"/api/enrollments/{enrollment.Id}",
            response);
    }

    /// <summary>
    /// Lists the logged-in Participant's own enrolments, newest first.
    /// </summary>
    /// <returns>The participant's enrolments.</returns>
    /// <response code="200">The enrolments were returned (the list may be empty).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organizer.</response>
    [HttpGet("api/enrollments/me")]
    public async Task<ActionResult<List<EnrollmentResponse>>>
        GetMyEnrollments()
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized("You must be logged in.");
        }

        var enrollments = await _context.Enrollments
            .Include(enrollment => enrollment.Event)
            .Include(enrollment => enrollment.Category)
            .Where(enrollment => enrollment.UserId == userId.Value)
            .OrderByDescending(enrollment => enrollment.EnrolledAt)
            .Select(enrollment => new EnrollmentResponse
            {
                Id = enrollment.Id,
                UserId = enrollment.UserId,
                EventId = enrollment.EventId,
                CategoryId = enrollment.CategoryId,
                EventName = enrollment.Event!.Name,
                CategoryName = enrollment.Category!.Name,
                EnrolledAt = enrollment.EnrolledAt
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    /// <summary>
    /// Cancels the logged-in Participant's enrolment in an event. Not possible once a result has been recorded.
    /// </summary>
    /// <param name="eventId">The ID of the event to leave.</param>
    /// <returns>No content when the enrolment was cancelled.</returns>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organizer.</response>
    /// <response code="404">The participant is not enrolled in this event.</response>
    /// <response code="409">A result has already been recorded.</response>
    [HttpDelete("api/events/{eventId:int}/enrollments/me")]
    public async Task<IActionResult> CancelEnrollment(int eventId)
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized("You must be logged in.");
        }

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(existingEnrollment =>
                existingEnrollment.UserId == userId.Value &&
                existingEnrollment.EventId == eventId);

        if (enrollment == null)
        {
            return NotFound(
                "You are not enrolled in this event.");
        }

        // Once a result exists the enrolment is part of the official record and cannot be cancelled.
        bool hasResult = await _context.Results
            .AnyAsync(result =>
                result.UserId == userId.Value &&
                result.EventId == eventId);

        if (hasResult)
        {
            return Conflict(
                "An enrollment cannot be cancelled after a result has been recorded.");
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
    /// Loads an enrolment together with its event and category names for the response body.
    /// </summary>
    private async Task<EnrollmentResponse?> BuildResponse(
        int enrollmentId)
    {
        return await _context.Enrollments
            .Include(enrollment => enrollment.Event)
            .Include(enrollment => enrollment.Category)
            .Where(enrollment => enrollment.Id == enrollmentId)
            .Select(enrollment => new EnrollmentResponse
            {
                Id = enrollment.Id,
                UserId = enrollment.UserId,
                EventId = enrollment.EventId,
                CategoryId = enrollment.CategoryId,
                EventName = enrollment.Event!.Name,
                CategoryName = enrollment.Category!.Name,
                EnrolledAt = enrollment.EnrolledAt
            })
            .FirstOrDefaultAsync();
    }
}