using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Parent class of all controllers. It holds the things every controller needs:
/// the ApiService, the logged-in user's ID and a way to show messages to the user.
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly ApiService Api;

    protected BaseController(ApiService api)
    {
        Api = api;
    }

    /// <summary>ID of the logged-in user (0 when nobody is logged in).</summary>
    protected int CurrentUserId => HttpContext.Session.GetUserId();

    /// <summary>Shows a green message on the next page (TempData survives one redirect).</summary>
    protected void ShowSuccess(string message)
    {
        TempData["Success"] = message;
    }

    /// <summary>Shows a red message on the next page.</summary>
    protected void ShowError(string message)
    {
        TempData["Error"] = message;
    }

    /// <summary>
    /// Loads an event and checks that it belongs to the logged-in organiser.
    /// Returns null (and sets an error message) when the event is missing or belongs to someone else.
    /// </summary>
    protected async Task<EventModel?> LoadOwnedEventAsync(int eventId)
    {
        ApiResponse<EventModel> response = await Api.GetAsync<EventModel>($"api/events/{eventId}");

        if (!response.IsSuccess || response.Data == null)
        {
            ShowError(response.ErrorMessage);
            return null;
        }

        if (response.Data.OrganizerId != CurrentUserId)
        {
            ShowError("You can only manage your own events.");
            return null;
        }

        return response.Data;
    }

    /// <summary>
    /// Loads everybody who enrolled for an event together with their results (if any) and works out
    /// each enrolment's status. Used by both the Enrolments page and the Results page.
    /// Returns null (and sets an error message) when the API call fails.
    /// </summary>
    protected async Task<EventParticipantsViewModel?> LoadEventParticipantsAsync(EventModel raceEvent)
    {
        ApiResponse<List<OrganizerEnrollmentModel>> enrolments =
            await Api.GetAsync<List<OrganizerEnrollmentModel>>($"api/organizer/events/{raceEvent.Id}/enrollments");

        if (!enrolments.IsSuccess || enrolments.Data == null)
        {
            ShowError(enrolments.ErrorMessage);
            return null;
        }

        // Event results are public, so any logged-in user may read them.
        ApiResponse<List<ResultModel>> results =
            await Api.GetAsync<List<ResultModel>>($"api/events/{raceEvent.Id}/results");

        List<ResultModel> eventResults = results.Data ?? new List<ResultModel>();

        var model = new EventParticipantsViewModel { Event = raceEvent };

        foreach (OrganizerEnrollmentModel enrolment in enrolments.Data)
        {
            // A result belongs to the same participant AND the same category as the enrolment.
            ResultModel? result = eventResults.FirstOrDefault(r =>
                r.UserId == enrolment.UserId && r.CategoryId == enrolment.CategoryId);

            model.Rows.Add(new EnrolmentRow
            {
                Enrolment = enrolment,
                Result = result,
                Status = DisplayHelper.EnrolmentStatus(raceEvent.EventDate, result != null, DateTime.Now)
            });
        }

        return model;
    }
}
