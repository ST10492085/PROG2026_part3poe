using System.ComponentModel.DataAnnotations;
using PROG2026_part3.Models.Api;

namespace PROG2026_part3.Models.ViewModels;

/// <summary>Data for the home page: the (filtered) events plus the filter values the user chose.</summary>
public class HomeViewModel
{
    public List<EventModel> Events { get; set; } = new();

    // The current filter values (so the form remembers them after searching).
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool ShowPast { get; set; }
}

/// <summary>Data for the event details page.</summary>
public class EventDetailsViewModel
{
    public EventModel Event { get; set; } = new();

    /// <summary>The logged-in participant's enrolment in this event (null when not enrolled / not a participant).</summary>
    public EnrollmentModel? MyEnrollment { get; set; }
}

/// <summary>The form used by organisers to create or edit an event.</summary>
public class EventFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a description.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date and time.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date and time")]
    public DateTime EventDate { get; set; } = DateTime.Now.AddDays(30);

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; } = 10;

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    /// <summary>Optional banner image chosen by the organiser. It is uploaded to Azure through the API.</summary>
    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    /// <summary>The banner that is already saved (shown on the edit page).</summary>
    public string? ExistingBannerUrl { get; set; }
}
