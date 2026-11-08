using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// The public home page: all upcoming events with filtering options.
/// </summary>
public class HomeController : BaseController
{
    public HomeController(ApiService api) : base(api)
    {
    }

    public async Task<IActionResult> Index(
        string? search,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        bool showPast = false)
    {
        var model = new HomeViewModel
        {
            Search = search,
            EventType = eventType,
            FromDate = fromDate,
            ToDate = toDate,
            ShowPast = showPast
        };

        ApiResponse<List<EventModel>> response = await Api.GetAsync<List<EventModel>>("api/events");

        if (!response.IsSuccess || response.Data == null)
        {
            ViewBag.ApiError = response.ErrorMessage;
            return View(model);
        }

        // The API returns every event. We filter them here, step by step.
        IEnumerable<EventModel> events = response.Data;

        // 1) Hide events that already happened (unless the user ticked "include past events").
        if (!showPast)
        {
            events = events.Where(e => e.EventDate.Date >= DateTime.Now.Date);
        }

        // 2) Text search in the name, location and description.
        if (!string.IsNullOrWhiteSpace(search))
        {
            string text = search.Trim().ToLower();

            events = events.Where(e =>
                e.Name.ToLower().Contains(text) ||
                e.Location.ToLower().Contains(text) ||
                e.Description.ToLower().Contains(text));
        }

        // 3) Event type: Run, Walk or Cycle.
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            events = events.Where(e => e.EventType == eventType);
        }

        // 4) Date range.
        if (fromDate.HasValue)
        {
            events = events.Where(e => e.EventDate.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            events = events.Where(e => e.EventDate.Date <= toDate.Value.Date);
        }

        model.Events = events.OrderBy(e => e.EventDate).ToList();

        return View(model);
    }

    public IActionResult Error()
    {
        return View();
    }
}
