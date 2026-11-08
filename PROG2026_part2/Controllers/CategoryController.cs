using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Manages the age / distance categories of an event.
/// Everyone can view categories; only the event's Organizer can create, change or delete them.
/// </summary>
[ApiController]
[Route("api/events/{eventId:int}/categories")]
public class CategoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all categories of an event, ordered by name. Available to everyone (no login needed).
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>A list of the event's categories.</returns>
    /// <response code="200">The categories were returned (the list may be empty).</response>
    /// <response code="404">The event does not exist.</response>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories(
        int eventId)
    {
        bool eventExists = await _context.Events
            .AnyAsync(raceEvent => raceEvent.Id == eventId);

        if (!eventExists)
        {
            return NotFound("Event was not found.");
        }

        var categories = await _context.Categories
            .Where(category => category.EventId == eventId)
            .OrderBy(category => category.Name)
            .Select(category => ToResponse(category))
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Returns a single category of an event. Available to everyone (no login needed).
    /// </summary>
    /// <param name="eventId">The ID of the event the category belongs to.</param>
    /// <param name="categoryId">The ID of the category.</param>
    /// <returns>The requested category.</returns>
    /// <response code="200">The category was returned.</response>
    /// <response code="404">The category does not exist for this event.</response>
    [AllowAnonymous]
    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(
        int eventId,
        int categoryId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(existingCategory =>
                existingCategory.Id == categoryId &&
                existingCategory.EventId == eventId);

        if (category == null)
        {
            return NotFound("Category was not found.");
        }

        return Ok(ToResponse(category));
    }

    /// <summary>
    /// Creates a category for an event. Organizer only, and only for events the Organizer owns.
    /// </summary>
    /// <param name="eventId">The ID of the event the category is added to.</param>
    /// <param name="request">Name (unique within the event), description, optional min/max age and optional distance in km.</param>
    /// <returns>The newly created category.</returns>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed, or MinAge is greater than MaxAge.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">A category with this name already exists for the event.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        int eventId,
        CreateCategoryRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(existingEvent =>
                existingEvent.Id == eventId);

        if (raceEvent == null)
        {
            return NotFound("Event was not found.");
        }

        // Organizers may only change categories of their own events.
        if (!IsCurrentUser(raceEvent.OrganizerId))
        {
            return Forbid();
        }

        // Sanity check: the minimum age cannot be higher than the maximum age.
        if (!AreAgeLimitsValid(request.MinAge, request.MaxAge))
        {
            return BadRequest(
                "MinAge cannot be greater than MaxAge.");
        }

        // Category names must be unique within one event (case-insensitive).
        bool duplicateName = await _context.Categories
            .AnyAsync(category =>
                category.EventId == eventId &&
                category.Name.ToLower() == request.Name.Trim().ToLower());

        if (duplicateName)
        {
            return Conflict(
                "A category with this name already exists for this event.");
        }

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // 201 Created + a Location header that points to the new category.
        return CreatedAtAction(
            nameof(GetCategory),
            new
            {
                eventId,
                categoryId = category.Id
            },
            ToResponse(category));
    }

    /// <summary>
    /// Replaces the details of a category. Organizer only, and only for events the Organizer owns.
    /// </summary>
    /// <param name="eventId">The ID of the event the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to update.</param>
    /// <param name="request">The new name, description, age limits and distance.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed, or MinAge is greater than MaxAge.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The category does not exist for this event.</response>
    /// <response code="409">Another category in this event already uses that name.</response>
    [Authorize(Roles = "Organizer")]
    [HttpPut("{categoryId:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        int eventId,
        int categoryId,
        UpdateCategoryRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await _context.Categories
            .Include(existingCategory => existingCategory.Event)
            .FirstOrDefaultAsync(existingCategory =>
                existingCategory.Id == categoryId &&
                existingCategory.EventId == eventId);

        if (category == null)
        {
            return NotFound("Category was not found.");
        }

        // Organizers may only change categories of their own events.
        if (category.Event == null ||
            !IsCurrentUser(category.Event.OrganizerId))
        {
            return Forbid();
        }

        // Sanity check: the minimum age cannot be higher than the maximum age.
        if (!AreAgeLimitsValid(request.MinAge, request.MaxAge))
        {
            return BadRequest(
                "MinAge cannot be greater than MaxAge.");
        }

        // Same uniqueness rule, ignoring the category that is being edited.
        bool duplicateName = await _context.Categories
            .AnyAsync(existingCategory =>
                existingCategory.EventId == eventId &&
                existingCategory.Id != categoryId &&
                existingCategory.Name.ToLower() ==
                request.Name.Trim().ToLower());

        if (duplicateName)
        {
            return Conflict(
                "A category with this name already exists for this event.");
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(category));
    }

    /// <summary>
    /// Deletes a category. Organizer only, and only for events the Organizer owns.
    /// A category that already has enrolments or results cannot be deleted.
    /// </summary>
    /// <param name="eventId">The ID of the event the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to delete.</param>
    /// <returns>No content when the category was deleted.</returns>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant, or the event belongs to another Organizer.</response>
    /// <response code="404">The category does not exist for this event.</response>
    /// <response code="409">The category has enrolments or results.</response>
    [Authorize(Roles = "Organizer")]
    [HttpDelete("{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(
        int eventId,
        int categoryId)
    {
        var category = await _context.Categories
            .Include(existingCategory => existingCategory.Event)
            .FirstOrDefaultAsync(existingCategory =>
                existingCategory.Id == categoryId &&
                existingCategory.EventId == eventId);

        if (category == null)
        {
            return NotFound("Category was not found.");
        }

        // Organizers may only change categories of their own events.
        if (category.Event == null ||
            !IsCurrentUser(category.Event.OrganizerId))
        {
            return Forbid();
        }

        // Protect existing enrolments / results from being orphaned.
        bool hasEnrollments = await _context.Enrollments
            .AnyAsync(enrollment =>
                enrollment.CategoryId == categoryId);

        bool hasResults = await _context.Results
            .AnyAsync(result =>
                result.CategoryId == categoryId);

        if (hasEnrollments || hasResults)
        {
            return Conflict(
                "This category cannot be deleted because it has enrollments or results.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Checks whether the logged-in user (from the session claims) is the given organizer.
    /// </summary>
    private bool IsCurrentUser(int organizerId)
    {
        string? currentUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(currentUserId, out int userId) &&
               userId == organizerId;
    }

    /// <summary>
    /// Age limits are valid when either one is missing, or MinAge is not greater than MaxAge.
    /// </summary>
    private static bool AreAgeLimitsValid(
        int? minAge,
        int? maxAge)
    {
        return !minAge.HasValue ||
               !maxAge.HasValue ||
               minAge.Value <= maxAge.Value;
    }

    /// <summary>
    /// Maps a Category entity to the DTO returned to API callers.
    /// </summary>
    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            EventId = category.EventId,
            Name = category.Name,
            Description = category.Description,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        };
    }
}