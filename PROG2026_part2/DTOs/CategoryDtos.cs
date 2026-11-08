using System.ComponentModel.DataAnnotations;

namespace PROG2026_part2.DTOs;

/// <summary>
/// Data required to create a category for an event.
/// </summary>
public class CreateCategoryRequest
{
    /// <summary>Category name, e.g. "Under 20" or "10K Open" (required, unique within the event).</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Short description of who the category is for.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Youngest allowed age (optional, 0-150).</summary>
    [Range(0, 150)]
    public int? MinAge { get; set; }

    /// <summary>Oldest allowed age (optional, 0-150, must not be less than MinAge).</summary>
    [Range(0, 150)]
    public int? MaxAge { get; set; }

    /// <summary>Distance in kilometres for this category (optional, 0.1-1000).</summary>
    [Range(0.1, 1000)]
    public decimal? DistanceKm { get; set; }
}

/// <summary>
/// Data used to replace an existing category's details.
/// </summary>
public class UpdateCategoryRequest
{
    /// <summary>Category name (required, unique within the event).</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Short description of who the category is for.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Youngest allowed age (optional, 0-150).</summary>
    [Range(0, 150)]
    public int? MinAge { get; set; }

    /// <summary>Oldest allowed age (optional, 0-150, must not be less than MinAge).</summary>
    [Range(0, 150)]
    public int? MaxAge { get; set; }

    /// <summary>Distance in kilometres (optional, 0.1-1000).</summary>
    [Range(0.1, 1000)]
    public decimal? DistanceKm { get; set; }
}

/// <summary>
/// A category as returned by the API.
/// </summary>
public class CategoryResponse
{
    public int Id { get; set; }

    /// <summary>The event the category belongs to.</summary>
    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public decimal? DistanceKm { get; set; }
}
