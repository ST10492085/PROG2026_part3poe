using System.ComponentModel.DataAnnotations;

namespace PROG2026_part2.DTOs;

/// <summary>
/// Data required to create an event.
/// </summary>
public class CreateEventRequest
{
    /// <summary>Event name (required, max 150 characters).</summary>
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>What the event is about (required).</summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>Date and time the event takes place (required, ISO 8601).</summary>
    [Required]
    public DateTime EventDate { get; set; }

    /// <summary>Where the event is held (required, max 150 characters).</summary>
    [Required]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Distance in kilometres (0.1-1000).</summary>
    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>The event type: "Run", "Walk" or "Cycle".</summary>
    [Required]
    public string EventType { get; set; } = "Run";
}

/// <summary>
/// Data used to update an event. Only the fields that are supplied are changed.
/// </summary>
public class UpdateEventRequest
{
    /// <summary>New name (optional, max 150 characters).</summary>
    [StringLength(150)]
    public string? Name { get; set; }

    /// <summary>New description (optional).</summary>
    public string? Description { get; set; }

    /// <summary>New date and time (optional).</summary>
    public DateTime? EventDate { get; set; }

    /// <summary>New location (optional, max 150 characters).</summary>
    [StringLength(150)]
    public string? Location { get; set; }

    /// <summary>New distance in kilometres (optional, 0.1-1000).</summary>
    [Range(0.1, 1000)]
    public decimal? DistanceKm { get; set; }

    /// <summary>New event type (optional): "Run", "Walk" or "Cycle".</summary>
    public string? EventType { get; set; }
}

/// <summary>
/// An event as returned by the API, including its categories.
/// </summary>
public class EventResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    /// <summary>"Run", "Walk" or "Cycle".</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>The ID of the Organizer who owns the event.</summary>
    public int OrganizerId { get; set; }

    /// <summary>URL of the banner image in Azure Blob Storage (null when none was uploaded).</summary>
    public string? BannerImageUrl { get; set; }

    public List<CategoryResponse> Categories { get; set; } = new();
}
