using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// The public event details page: full information, banner image and the available categories.
/// A logged-in participant can enrol from here.
/// </summary>
public class EventsController : BaseController
{
    public EventsController(ApiService api) : base(api)
    {
    }

    public async Task<IActionResult> Details(int id)
    {
        ApiResponse<EventModel> response = await Api.GetAsync<EventModel>($"api/events/{id}");

        if (!response.IsSuccess || response.Data == null)
        {
            ShowError(response.ErrorMessage);
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel { Event = response.Data };

        // For a logged-in participant, check whether they are already enrolled in this event.
        if (HttpContext.Session.GetRole() == "Participant")
        {
            ApiResponse<List<EnrollmentModel>> mine =
                await Api.GetAsync<List<EnrollmentModel>>("api/enrollments/me");

            if (mine.IsSuccess && mine.Data != null)
            {
                model.MyEnrollment = mine.Data.FirstOrDefault(e => e.EventId == id);
            }
        }

        return View(model);
    }
}
