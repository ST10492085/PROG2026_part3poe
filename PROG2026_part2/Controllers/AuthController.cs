using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;
using PROG2026_part2.Models;
using PROG2026_part2.Services;

namespace PROG2026_part2.Controllers;

/// <summary>
/// Handles registration, login and logout. Login stores the user's ID and role in the
/// server-side session, which every protected endpoint later relies on.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Database access and password hashing are injected by ASP.NET Core (dependency injection).
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;

    public AuthController(
        ApplicationDbContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Registers a new RaceDay account as either an Organizer or a Participant.
    /// The password is hashed before it is stored.
    /// </summary>
    /// <param name="request">First name, last name, email, password (min 6 characters) and role ("Organizer" or "Participant").</param>
    /// <returns>The newly created user's basic details (never the password).</returns>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed, or the role is not Organizer/Participant.</response>
    /// <response code="409">An account with this email already exists.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Emails are stored in lower case so the unique-email check is case-insensitive.
        string email = request.Email.Trim().ToLower();

        string role = request.Role.Trim();

        // Only the two supported roles may be chosen at registration.
        if (!role.Equals("Organizer", StringComparison.OrdinalIgnoreCase) &&
            !role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Role must be either Organizer or Participant.");
        }

        // Normalise to "Organizer" / "Participant" so role checks always match.
        role = char.ToUpper(role[0]) + role[1..].ToLower();

        // Each email may only be registered once.
        bool emailExists = await _context.Users
            .AnyAsync(user => user.Email == email);

        if (emailExists)
        {
            return Conflict("An account with this email already exists.");
        }

        // Hash + salt the password; the original password is never saved.
        PasswordResult passwordResult =
            _passwordService.CreatePasswordHash(request.Password);

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = passwordResult.Hash,
            PasswordSalt = passwordResult.Salt,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var response = new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Message = "Registration successful."
        };

        return Created(
            $"/api/users/{user.Id}",
            response);
    }

    /// <summary>
    /// Logs in with email and password. On success the user's ID and role are stored in the server-side session
    /// and a session cookie is returned, which authenticates all later requests.
    /// </summary>
    /// <param name="request">The registered email and password.</param>
    /// <returns>The logged-in user's basic details.</returns>
    /// <response code="200">Login successful; session cookie issued.</response>
    /// <response code="400">Validation failed (missing or malformed email/password).</response>
    /// <response code="401">Invalid email or password.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request)
    {
        // Reject the request when the DTO's validation attributes failed.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        string email = request.Email.Trim().ToLower();

        // Look the user up by email.
        ApplicationUser? user = await _context.Users
            .FirstOrDefaultAsync(existingUser =>
                existingUser.Email == email);

        // The same message is used for 'unknown email' and 'wrong password' so attackers
        // cannot tell which emails are registered.
        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Re-hash the supplied password with the stored salt and compare.
        bool passwordIsValid = _passwordService.VerifyPassword(
            request.Password,
            user.PasswordHash,
            user.PasswordSalt);

        if (!passwordIsValid)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Store authentication information in the server-side session.
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Message = "Login successful."
        });
    }

    /// <summary>
    /// Logs out the current user by clearing the server-side session.
    /// </summary>
    /// <returns>A confirmation message.</returns>
    /// <response code="200">Logout successful; the session cookie is no longer valid.</response>
    [HttpPost("logout")]
    public ActionResult Logout()
    {
        // Removes the stored user ID and role, so protected endpoints return 401 again.
        HttpContext.Session.Clear();

        return Ok(new
        {
            Message = "Logout successful."
        });
    }
}