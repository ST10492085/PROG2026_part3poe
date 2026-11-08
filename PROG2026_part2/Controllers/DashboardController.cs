using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Role-specific dashboards: a Participant sees their enrolments and results,
/// an Organizer sees summary figures for the events they own.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns dashboard information (enrolments and results) for the logged-in Participant.
    /// </summary>
    /// <returns>The participant's dashboard.</returns>
    /// <response code="200">The dashboard was returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organizer.</response>
    /// <response code="404">The participant no longer exists.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("participant")]
    public async Task<ActionResult<ParticipantDashboardResponse>>
        GetParticipantDashboard()
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(
                "You must be logged in as a Participant.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(existingUser =>
                existingUser.Id == userId.Value);

        if (user == null)
        {
            return NotFound("Participant was not found.");
        }

        // Only this participant's own enrolments, soonest event first.
        var enrollments = await _context.Enrollments
            .Include(enrollment => enrollment.Event)
            .Include(enrollment => enrollment.Category)
            .Where(enrollment => enrollment.UserId == userId.Value)
            .OrderBy(enrollment => enrollment.Event!.EventDate)
            .ToListAsync();

        // Only this participant's own results, most recent first.
        var results = await _context.Results
            .Include(result => result.User)
            .Include(result => result.Event)
            .Include(result => result.Category)
            .Where(result => result.UserId == userId.Value)
            .OrderByDescending(result => result.RecordedAt)
            .ToListAsync();

        var response = new ParticipantDashboardResponse
        {
            UserId = user.Id,
            ParticipantName = $"{user.FirstName} {user.LastName}",
            EnrollmentCount = enrollments.Count,
            ResultCount = results.Count,
            Enrollments = enrollments
                .Select(ToEnrollmentResponse)
                .ToList(),
            Results = results
                .Select(ToResultResponse)
                .ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Returns dashboard information (events with enrolment and result counts) for the logged-in Organizer.
    /// </summary>
    /// <returns>The organizer's dashboard.</returns>
    /// <response code="200">The dashboard was returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    /// <response code="404">The organizer no longer exists.</response>
    [Authorize(Roles = "Organizer")]
    [HttpGet("organizer")]
    public async Task<ActionResult<OrganizerDashboardResponse>>
        GetOrganizerDashboard()
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? organizerId = GetCurrentUserId();

        if (!organizerId.HasValue)
        {
            return Unauthorized(
                "You must be logged in as an Organizer.");
        }

        var organizer = await _context.Users
            .FirstOrDefaultAsync(existingUser =>
                existingUser.Id == organizerId.Value);

        if (organizer == null)
        {
            return NotFound("Organizer was not found.");
        }

        // Only events owned by this organizer, with enrolments/results loaded for the counts.
        var events = await _context.Events
            .Where(raceEvent =>
                raceEvent.OrganizerId == organizerId.Value)
            .Include(raceEvent => raceEvent.Enrollments)
            .Include(raceEvent => raceEvent.Results)
            .OrderBy(raceEvent => raceEvent.EventDate)
            .ToListAsync();

        var response = new OrganizerDashboardResponse
        {
            UserId = organizer.Id,
            OrganizerName =
                $"{organizer.FirstName} {organizer.LastName}",
            EventCount = events.Count,
            EnrollmentCount = events
                .Sum(raceEvent => raceEvent.Enrollments.Count),
            ResultCount = events
                .Sum(raceEvent => raceEvent.Results.Count),
            Events = events
                .Select(raceEvent => new OrganizerEventSummary
                {
                    EventId = raceEvent.Id,
                    EventName = raceEvent.Name,
                    EventDate = raceEvent.EventDate,
                    Location = raceEvent.Location,
                    EnrollmentCount =
                        raceEvent.Enrollments.Count,
                    ResultCount =
                        raceEvent.Results.Count
                })
                .ToList()
        };

        return Ok(response);
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
    /// Maps an Enrollment entity to the DTO shown on the dashboard.
    /// </summary>
    private static EnrollmentResponse ToEnrollmentResponse(
        Enrollment enrollment)
    {
        return new EnrollmentResponse
        {
            Id = enrollment.Id,
            UserId = enrollment.UserId,
            EventId = enrollment.EventId,
            CategoryId = enrollment.CategoryId,
            EventName = enrollment.Event?.Name ?? string.Empty,
            CategoryName = enrollment.Category?.Name ?? string.Empty,
            EnrolledAt = enrollment.EnrolledAt
        };
    }

    /// <summary>
    /// Maps a Result entity to the DTO shown on the dashboard.
    /// </summary>
    private static ResultResponse ToResultResponse(Result result)
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
            EventName = result.Event?.Name ?? string.Empty,
            CategoryName = result.Category?.Name ?? string.Empty,
            FinishTime = result.FinishTime,
            Position = result.Position,
            RecordedAt = result.RecordedAt
        };
    }
}