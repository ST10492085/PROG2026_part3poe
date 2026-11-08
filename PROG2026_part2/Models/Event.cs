namespace PROG2026_part2.Models;

/// <summary>
/// A race event created by an Organizer.
/// </summary>
public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }

    /// <summary>Public URL of the banner image stored in Azure Blob Storage (null = no banner).</summary>
    public string? BannerImageUrl { get; set; }

    /// <summary>One of "Run", "Walk" or "Cycle".</summary>
    public string EventType { get; set; } = "Run";

    // Foreign key: the Organizer who owns (and may change/delete) this event.
    public int OrganizerId { get; set; }
    public ApplicationUser? Organizer { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
