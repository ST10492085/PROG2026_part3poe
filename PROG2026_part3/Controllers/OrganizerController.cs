using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// The organiser dashboard: a summary of all events the organiser has created,
/// total enrolments per event and the upcoming event dates.
/// </summary>
[SessionAuthorize("Organizer")]
public class OrganizerController : BaseController
{
    public OrganizerController(ApiService api) : base(api)
    {
    }

    public async Task<IActionResult> Dashboard()
    {
        ApiResponse<OrganizerDashboardModel> response =
            await Api.GetAsync<OrganizerDashboardModel>("api/dashboard/organizer");

        if (!response.IsSuccess || response.Data == null)
        {
            ViewBag.ApiError = response.ErrorMessage;
            return View(new OrganizerDashboardModel());
        }

        return View(response.Data);
    }
}
