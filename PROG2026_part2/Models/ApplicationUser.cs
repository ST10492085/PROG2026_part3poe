namespace PROG2026_part2.Models;

/// <summary>
/// A registered RaceDay user. The same table holds both roles:
/// "Organizer" (manages events) and "Participant" (enters events).
/// </summary>
public class ApplicationUser
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Unique, stored in lower case; used as the login name.</summary>
    public string Email { get; set; } = string.Empty;

    // The password is never stored in its original form: only a salted PBKDF2 hash.
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Either "Organizer" or "Participant".</summary>
    public string Role { get; set; } = "Participant";

    // Optional profile details the user can add later.
    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>Public URL of the profile picture stored in Azure Blob Storage (null = no picture).</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (one user -> many enrollments / results).
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
