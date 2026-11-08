using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Shows an ORGANISER every participant who enrolled for one of their events,
/// with the category they chose and the enrolment status.
/// </summary>
[SessionAuthorize("Organizer")]
public class OrganizerEnrolmentsController : BaseController
{
    public OrganizerEnrolmentsController(ApiService api) : base(api)
    {
    }

    public async Task<IActionResult> Index(int eventId)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(eventId);

        if (raceEvent == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        EventParticipantsViewModel? model = await LoadEventParticipantsAsync(raceEvent);

        if (model == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        return View(model);
    }
}
