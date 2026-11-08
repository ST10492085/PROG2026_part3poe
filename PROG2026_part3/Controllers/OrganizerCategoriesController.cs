using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Category management for ORGANISERS: add, edit and delete the age / distance categories of an event.
/// </summary>
[SessionAuthorize("Organizer")]
public class OrganizerCategoriesController : BaseController
{
    public OrganizerCategoriesController(ApiService api) : base(api)
    {
    }

    // ----- List the categories of one event -----

    public async Task<IActionResult> Index(int eventId)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(eventId);

        if (raceEvent == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        return View(raceEvent);
    }

    // ----- Add a category -----

    [HttpGet]
    public async Task<IActionResult> Create(int eventId)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(eventId);

        if (raceEvent == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        return View(new CategoryFormViewModel
        {
            EventId = eventId,
            EventName = raceEvent.Name,
            DistanceKm = raceEvent.DistanceKm
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        // The minimum age cannot be higher than the maximum age.
        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
        {
            ModelState.AddModelError(nameof(model.MinAge), "The minimum age cannot be higher than the maximum age.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApiResponse<CategoryModel> response = await Api.PostAsync<CategoryModel>(
            $"api/events/{model.EventId}/categories",
            BuildCategoryRequest(model));

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        ShowSuccess("Category added.");
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    // ----- Edit a category -----

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        EventModel? raceEvent = await LoadOwnedEventAsync(eventId);

        if (raceEvent == null)
        {
            return RedirectToAction("Index", "OrganizerEvents");
        }

        CategoryModel? category = raceEvent.Categories.FirstOrDefault(c => c.Id == id);

        if (category == null)
        {
            ShowError("That category was not found.");
            return RedirectToAction("Index", new { eventId });
        }

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            EventId = eventId,
            EventName = raceEvent.Name,
            Name = category.Name,
            Description = category.Description,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
        {
            ModelState.AddModelError(nameof(model.MinAge), "The minimum age cannot be higher than the maximum age.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApiResponse<CategoryModel> response = await Api.PutAsync<CategoryModel>(
            $"api/events/{model.EventId}/categories/{model.Id}",
            BuildCategoryRequest(model));

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        ShowSuccess("Category updated.");
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    // ----- Delete a category -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        ApiResponse response = await Api.DeleteAsync($"api/events/{eventId}/categories/{id}");

        if (response.IsSuccess)
        {
            ShowSuccess("Category deleted.");
        }
        else
        {
            // For example: the category already has enrolments.
            ShowError(response.ErrorMessage);
        }

        return RedirectToAction("Index", new { eventId });
    }

    // ----- Helper -----

    /// <summary>Builds the JSON the API expects for creating or updating a category.</summary>
    private static object BuildCategoryRequest(CategoryFormViewModel model)
    {
        return new
        {
            name = model.Name.Trim(),
            description = (model.Description ?? string.Empty).Trim(),
            minAge = model.MinAge,
            maxAge = model.MaxAge,
            distanceKm = model.DistanceKm
        };
    }
}
