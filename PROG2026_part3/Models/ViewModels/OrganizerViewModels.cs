using System.ComponentModel.DataAnnotations;
using PROG2026_part3.Models.Api;

namespace PROG2026_part3.Models.ViewModels;

/// <summary>The form used to add or edit a category.</summary>
public class CategoryFormViewModel
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

/// <summary>One row on the organiser's enrolments / results pages.</summary>
public class EnrolmentRow
{
    public OrganizerEnrollmentModel Enrolment { get; set; } = new();

    /// <summary>"Confirmed", "Awaiting result" or "Completed".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The participant's result, or null when none has been captured yet.</summary>
    public ResultModel? Result { get; set; }
}

/// <summary>Data for the organiser's enrolments page and results page (same shape, different view).</summary>
public class EventParticipantsViewModel
{
    public EventModel Event { get; set; } = new();
    public List<EnrolmentRow> Rows { get; set; } = new();
}

/// <summary>The form used by organisers to capture or correct a result.</summary>
public class ResultFormViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    /// <summary>Only used when editing an existing result.</summary>
    public int ResultId { get; set; }

    public int UserId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finish time as hh:mm:ss.")]
    [Display(Name = "Finish time (hh:mm:ss)")]
    public TimeSpan FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "The position must be 1 or higher.")]
    [Display(Name = "Finishing position")]
    public int Position { get; set; } = 1;
}
