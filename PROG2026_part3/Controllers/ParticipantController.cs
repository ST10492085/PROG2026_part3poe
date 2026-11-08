using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Pages for logged-in PARTICIPANTS: enrol in an event, see "My Events" and "My Results".
/// </summary>
[SessionAuthorize("Participant")]
public class ParticipantController : BaseController
{
    public ParticipantController(ApiService api) : base(api)
    {
    }

    // ----- Enrol in an event -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            ShowError("Please choose a category before enrolling.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        // The API records the link between the participant, the event and the chosen category.
        ApiResponse<EnrollmentModel> response = await Api.PostAsync<EnrollmentModel>(
            $"api/events/{eventId}/enrollments",
            new { categoryId });

        if (!response.IsSuccess)
        {
            ShowError(response.ErrorMessage);
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        ShowSuccess("You are enrolled! Your enrolment has been recorded.");
        return RedirectToAction("MyEvents");
    }

    // ----- My Events -----

    public async Task<IActionResult> MyEvents()
    {
        var items = new List<MyEventItem>();

        // Three API calls: my enrolments, all events (for dates/locations) and my results (for status).
        ApiResponse<List<EnrollmentModel>> enrolments =
            await Api.GetAsync<List<EnrollmentModel>>("api/enrollments/me");

        if (!enrolments.IsSuccess || enrolments.Data == null)
        {
            ViewBag.ApiError = enrolments.ErrorMessage;
            return View(items);
        }

        ApiResponse<List<EventModel>> events = await Api.GetAsync<List<EventModel>>("api/events");
        ApiResponse<List<ResultModel>> results = await Api.GetAsync<List<ResultModel>>("api/results/me");

        List<EventModel> allEvents = events.Data ?? new List<EventModel>();
        List<ResultModel> myResults = results.Data ?? new List<ResultModel>();

        foreach (EnrollmentModel enrolment in enrolments.Data)
        {
            EventModel? raceEvent = allEvents.FirstOrDefault(e => e.Id == enrolment.EventId);
            DateTime eventDate = raceEvent?.EventDate ?? DateTime.MinValue;
            bool hasResult = myResults.Any(r => r.EventId == enrolment.EventId);

            string status = DisplayHelper.EnrolmentStatus(eventDate, hasResult, DateTime.Now);

            items.Add(new MyEventItem
            {
                Enrollment = enrolment,
                EventDate = eventDate,
                Location = raceEvent?.Location ?? string.Empty,
                Status = status,
                // Only enrolments for future events without a result can be cancelled.
                CanCancel = status == "Confirmed"
            });
        }

        // Soonest event first.
        return View(items.OrderBy(i => i.EventDate).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int eventId)
    {
        ApiResponse response = await Api.DeleteAsync($"api/events/{eventId}/enrollments/me");

        if (response.IsSuccess)
        {
            ShowSuccess("Your enrolment was cancelled.");
        }
        else
        {
            ShowError(response.ErrorMessage);
        }

        return RedirectToAction("MyEvents");
    }

    // ----- My Results -----

    public async Task<IActionResult> MyResults()
    {
        var items = new List<MyResultItem>();

        ApiResponse<List<ResultModel>> results = await Api.GetAsync<List<ResultModel>>("api/results/me");

        if (!results.IsSuccess || results.Data == null)
        {
            ViewBag.ApiError = results.ErrorMessage;
            return View(items);
        }

        ApiResponse<List<EventModel>> events = await Api.GetAsync<List<EventModel>>("api/events");
        List<EventModel> allEvents = events.Data ?? new List<EventModel>();

        foreach (ResultModel result in results.Data)
        {
            EventModel? raceEvent = allEvents.FirstOrDefault(e => e.Id == result.EventId);

            // "47th out of 312 finishers": count everybody who has a result in this event.
            ApiResponse<List<ResultModel>> eventResults =
                await Api.GetAsync<List<ResultModel>>($"api/events/{result.EventId}/results");

            items.Add(new MyResultItem
            {
                Result = result,
                EventDate = raceEvent?.EventDate ?? DateTime.MinValue,
                TotalFinishers = eventResults.Data?.Count ?? 1
            });
        }

        // Most recent race first.
        return View(items.OrderByDescending(i => i.EventDate).ToList());
    }
}
