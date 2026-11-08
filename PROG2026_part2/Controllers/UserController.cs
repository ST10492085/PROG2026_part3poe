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
/// Lets any logged-in user (Organizer or Participant) view and update their own profile.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]   // every endpoint requires a logged-in user, whatever their role
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UserController(
        ApplicationDbContext context,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Returns the profile of the currently logged-in user (both roles). The password is never returned.
    /// </summary>
    /// <returns>The user's profile.</returns>
    /// <response code="200">The profile was returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The user no longer exists.</response>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized("You must be logged in.");
        }

        ApplicationUser? user = await _context.Users
            .FirstOrDefaultAsync(existingUser =>
                existingUser.Id == userId.Value);

        if (user == null)
        {
            return NotFound("User profile was not found.");
        }

        return Ok(ToProfileResponse(user));
    }

    /// <summary>
    /// Updates the logged-in user's own profile. Only the supplied fields are changed; email and role cannot be changed.
    /// </summary>
    /// <param name="request">Any of: first name, last name, phone number, address.</param>
    /// <returns>The updated profile.</returns>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Validation failed (e.g. invalid phone number).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The user no longer exists.</response>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile(
        UpdateProfileRequest request)
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
            return Unauthorized("You must be logged in.");
        }

        ApplicationUser? user = await _context.Users
            .FirstOrDefaultAsync(existingUser =>
                existingUser.Id == userId.Value);

        if (user == null)
        {
            return NotFound("User profile was not found.");
        }

        // Partial update: a field is only changed when a value was supplied for it.
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.FirstName = request.FirstName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            user.LastName = request.LastName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            user.Phone = request.Phone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            user.Address = request.Address.Trim();
        }

        // Email and Role are intentionally not updated here.
        // This prevents users from changing their identity or permissions.

        await _context.SaveChangesAsync();

        return Ok(ToProfileResponse(user));
    }

    /// <summary>
    /// Uploads (or replaces) the logged-in user's profile picture. The image is stored in Azure Blob Storage
    /// and its URL is saved on the user's profile. Available to both roles.
    /// </summary>
    /// <param name="file">The image file (form field "file"): JPG, PNG, GIF or WEBP, maximum 5 MB.</param>
    /// <returns>The updated profile, including the new ProfilePictureUrl.</returns>
    /// <response code="200">Picture uploaded and the profile updated.</response>
    /// <response code="400">No file, file too large, or not an allowed image type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The user no longer exists.</response>
    /// <response code="503">Azure Blob Storage is not configured on the server.</response>
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UserProfileResponse>> UploadProfilePicture(IFormFile file)
    {
        // The user's ID comes from the session claims (set at login), never from the request body.
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized("You must be logged in.");
        }

        ApplicationUser? user = await _context.Users
            .FirstOrDefaultAsync(existingUser =>
                existingUser.Id == userId.Value);

        if (user == null)
        {
            return NotFound("User profile was not found.");
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

        // 1) Upload the new picture, 2) save its URL in the database, 3) delete the old picture.
        string? oldPictureUrl = user.ProfilePictureUrl;

        user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(file, "profiles");
        await _context.SaveChangesAsync();

        await _blobStorage.DeleteImageAsync(oldPictureUrl);

        return Ok(ToProfileResponse(user));
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
    /// Maps a user entity to the profile DTO (leaving out the password hash and salt).
    /// </summary>
    private static UserProfileResponse ToProfileResponse(
        ApplicationUser user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Phone = user.Phone,
            Address = user.Address,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt
        };
    }
}