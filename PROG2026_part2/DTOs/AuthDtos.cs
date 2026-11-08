using System.ComponentModel.DataAnnotations;

namespace PROG2026_part2.DTOs;

/// <summary>
/// Data required to register a new account.
/// </summary>
public class RegisterRequest
{
    /// <summary>The user's first name (required, max 50 characters).</summary>
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The user's last name (required, max 50 characters).</summary>
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>A valid, unique email address. Used as the login name.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>The password (at least 6 characters). It is stored hashed, never in plain text.</summary>
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>The role the user wants: either "Organizer" or "Participant".</summary>
    [Required]
    public string Role { get; set; } = "Participant";
}

/// <summary>
/// Credentials used to log in.
/// </summary>
public class LoginRequest
{
    /// <summary>The email address the account was registered with.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>The account password.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Returned after a successful registration or login.
/// </summary>
public class AuthResponse
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>"Organizer" or "Participant".</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>A short confirmation message.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Profile fields a user may change. Fields that are left out (null/empty) are not changed.
/// Email and Role cannot be changed here.
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>New first name (optional, max 50 characters).</summary>
    [StringLength(50)]
    public string? FirstName { get; set; }

    /// <summary>New last name (optional, max 50 characters).</summary>
    [StringLength(50)]
    public string? LastName { get; set; }

    /// <summary>New phone number (optional, must be a valid phone format).</summary>
    [Phone]
    public string? Phone { get; set; }

    /// <summary>New postal address (optional).</summary>
    public string? Address { get; set; }
}

/// <summary>
/// The logged-in user's profile. The password hash is never included.
/// </summary>
public class UserProfileResponse
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    /// <summary>URL of the profile picture in Azure Blob Storage (null when none was uploaded).</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
