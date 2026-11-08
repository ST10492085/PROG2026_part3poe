using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Results entry for ORGANISERS: capture, correct and delete the finish time and finishing position
/// of each enrolled participant. A saved result is published straight away, so the participant
/// can see it on their "My Results" page.
/// </summary>
[SessionAuthorize("Organizer")]
public class OrganizerResultsController : BaseController
{
    public OrganizerResultsController(ApiService api) : base(api)
    {
    }

    // ----- List everybody who enrolled, with their result (or an "Add result" button) -----

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

    // ----- Capture a result -----

    [HttpGet]
    public async Task<IActionResult> Create(int eventId, int userId, int categoryId)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(eventId);

        if (raceEvent == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        EventParticipantsViewModel? participants = await LoadEventParticipantsAsync(raceEvent);

        // Find the enrolment this result is for (so we can show the participant's name).
        EnrolmentRow? row = participants?.Rows.FirstOrDefault(r =>
            r.Enrolment.UserId == userId && r.Enrolment.CategoryId == categoryId);

        if (row == null)
        {
            ShowError("That participant is not enrolled in this category.");
            return RedirectToAction("Index", new { eventId });
        }

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EventName = raceEvent.Name,
            UserId = userId,
            ParticipantName = row.Enrolment.ParticipantName,
            CategoryId = categoryId,
            CategoryName = row.Enrolment.CategoryName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResultFormViewModel model)
    {
        // A race cannot be finished in zero (or negative) time.
        if (model.FinishTime <= TimeSpan.Zero)
        {
            ModelState.AddModelError(nameof(model.FinishTime), "The finish time must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApiResponse<ResultModel> response = await Api.PostAsync<ResultModel>(
            $"api/events/{model.EventId}/results",
            new
            {
                userId = model.UserId,
                categoryId = model.CategoryId,
                finishTime = model.FinishTime,
                position = model.Position
            });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        ShowSuccess("Result saved and published.");
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    // ----- Correct a result -----

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int resultId)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(eventId);

        if (raceEvent == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        ApiResponse<List<ResultModel>> results =
            await Api.GetAsync<List<ResultModel>>($"api/events/{eventId}/results");

        ResultModel? result = results.Data?.FirstOrDefault(r => r.Id == resultId);

        if (result == null)
        {
            ShowError("That result was not found.");
            return RedirectToAction("Index", new { eventId });
        }

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EventName = raceEvent.Name,
            ResultId = result.Id,
            UserId = result.UserId,
            ParticipantName = result.ParticipantName,
            CategoryId = result.CategoryId,
            CategoryName = result.CategoryName,
            FinishTime = result.FinishTime,
            Position = result.Position
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ResultFormViewModel model)
    {
        if (model.FinishTime <= TimeSpan.Zero)
        {
            ModelState.AddModelError(nameof(model.FinishTime), "The finish time must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApiResponse<ResultModel> response = await Api.PutAsync<ResultModel>(
            $"api/events/{model.EventId}/results/{model.ResultId}",
            new
            {
                finishTime = model.FinishTime,
                position = model.Position
            });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        ShowSuccess("Result updated.");
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    // ----- Delete a result -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int resultId)
    {
        ApiResponse response = await Api.DeleteAsync($"api/events/{eventId}/results/{resultId}");

        if (response.IsSuccess)
        {
            ShowSuccess("Result deleted.");
        }
        else
        {
            ShowError(response.ErrorMessage);
        }

        return RedirectToAction("Index", new { eventId });
    }
}
