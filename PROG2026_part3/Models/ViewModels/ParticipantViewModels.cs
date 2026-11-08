using PROG2026_part3.Models.Api;

namespace PROG2026_part3.Models.ViewModels;

/// <summary>One row on the "My Events" page.</summary>
public class MyEventItem
{
    public EnrollmentModel Enrollment { get; set; } = new();
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;

    /// <summary>"Confirmed", "Awaiting result" or "Completed".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>True while the event is still in the future and has no result.</summary>
    public bool CanCancel { get; set; }
}

/// <summary>One row on the "My Results" page.</summary>
public class MyResultItem
{
    public ResultModel Result { get; set; } = new();
    public DateTime EventDate { get; set; }

    /// <summary>How many people have a result in the same event (the "312" in "47th out of 312").</summary>
    public int TotalFinishers { get; set; }
}
