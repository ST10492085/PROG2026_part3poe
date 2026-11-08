using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Event management for ORGANISERS: list, create, edit and delete events,
/// including uploading an event banner image.
/// </summary>
[SessionAuthorize("Organizer")]
public class OrganizerEventsController : BaseController
{
    public OrganizerEventsController(ApiService api) : base(api)
    {
    }

    // ----- List my events -----

    public async Task<IActionResult> Index()
    {
        ApiResponse<List<EventModel>> response = await Api.GetAsync<List<EventModel>>("api/events");

        if (!response.IsSuccess || response.Data == null)
        {
            ViewBag.ApiError = response.ErrorMessage;
            return View(new List<EventModel>());
        }

        // The API lists everybody's events, so keep only the ones this organiser owns.
        List<EventModel> myEvents = response.Data
            .Where(e => e.OrganizerId == CurrentUserId)
            .OrderBy(e => e.EventDate)
            .ToList();

        return View(myEvents);
    }

    // ----- Create -----

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        // Check the optional banner image before anything is sent to the API.
        string? imageProblem = ImageFileChecker.Check(model.BannerFile);

        if (imageProblem != null)
        {
            ModelState.AddModelError(nameof(model.BannerFile), imageProblem);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Step 1: create the event.
        ApiResponse<EventModel> response =
            await Api.PostAsync<EventModel>("api/events", BuildEventRequest(model));

        if (!response.IsSuccess || response.Data == null)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        // Step 2: upload the banner (if one was chosen) for the event we just created.
        if (model.BannerFile != null && model.BannerFile.Length > 0)
        {
            ApiResponse<EventModel> upload = await Api.UploadFileAsync<EventModel>(
                $"api/events/{response.Data.Id}/banner",
                model.BannerFile);

            if (!upload.IsSuccess)
            {
                ShowError("The event was created, but the banner could not be uploaded: " + upload.ErrorMessage);
                return RedirectToAction("Edit", new { id = response.Data.Id });
            }
        }

        ShowSuccess("Event created. Now add some categories for it.");
        return RedirectToAction("Index", "OrganizerCategories", new { eventId = response.Data.Id });
    }

    // ----- Edit -----

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(id);

        if (raceEvent == null)
        {
            return RedirectToAction("Index");
        }

        var model = new EventFormViewModel
        {
            Id = raceEvent.Id,
            Name = raceEvent.Name,
            Description = raceEvent.Description,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            DistanceKm = raceEvent.DistanceKm,
            EventType = raceEvent.EventType,
            ExistingBannerUrl = raceEvent.BannerImageUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        string? imageProblem = ImageFileChecker.Check(model.BannerFile);

        if (imageProblem != null)
        {
            ModelState.AddModelError(nameof(model.BannerFile), imageProblem);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Step 1: save the changed details.
        ApiResponse<EventModel> response =
            await Api.PutAsync<EventModel>($"api/events/{id}", BuildEventRequest(model));

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        // Step 2: replace the banner image if a new one was chosen.
        if (model.BannerFile != null && model.BannerFile.Length > 0)
        {
            ApiResponse<EventModel> upload =
                await Api.UploadFileAsync<EventModel>($"api/events/{id}/banner", model.BannerFile);

            if (!upload.IsSuccess)
            {
                ShowError("The event was updated, but the banner could not be uploaded: " + upload.ErrorMessage);
                return RedirectToAction("Edit", new { id });
            }
        }

        ShowSuccess("Event updated.");
        return RedirectToAction("Index");
    }

    // ----- Delete -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        ApiResponse response = await Api.DeleteAsync($"api/events/{id}");

        if (response.IsSuccess)
        {
            ShowSuccess("Event deleted.");
        }
        else
        {
            // For example: "This event cannot be deleted because it has enrollments or results."
            ShowError(response.ErrorMessage);
        }

        return RedirectToAction("Index");
    }

    // ----- Helper -----

    /// <summary>Builds the JSON the API expects for creating or updating an event.</summary>
    private static object BuildEventRequest(EventFormViewModel model)
    {
        return new
        {
            name = model.Name.Trim(),
            description = model.Description.Trim(),
            eventDate = model.EventDate,
            location = model.Location.Trim(),
            distanceKm = model.DistanceKm,
            eventType = model.EventType
        };
    }
}
