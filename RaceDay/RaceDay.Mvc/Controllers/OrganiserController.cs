using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class OrganiserController : RaceDayControllerBase
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api)
    {
        _api = api;
    }

    // ================= Dashboard =================

    public async Task<IActionResult> Index()
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var eventsResponse = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        var expired = SessionExpired(eventsResponse);
        if (expired != null) return expired;

        var events = await _api.ReadAsync<List<EventDto>>(eventsResponse) ?? new();
        var vm = new OrganiserDashboardVm();

        foreach (var ev in events)
        {
            var enrolResponse = await _api.SendAsync(HttpMethod.Get, $"api/enrolments/event/{ev.EventId}");
            var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(enrolResponse) ?? new();

            vm.Events.Add(new OrganiserEventSummaryVm
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                Location = ev.Location,
                EventType = ev.EventType,
                EnrolmentCount = enrolments.Count(e => e.Status != "Cancelled")
            });
        }

        return View(vm);
    }

    // ================= Events =================

    public async Task<IActionResult> Events()
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        var events = await _api.ReadAsync<List<EventDto>>(response) ?? new();
        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        return View("EventForm", new EventFormVm());
    }

   [HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> CreateEvent(EventFormVm vm)
{
    var guard = RequireRole("Organiser");
    if (guard != null) return guard;

    if (!ModelState.IsValid) return View("EventForm", vm);

    var response = await _api.SendAsync(HttpMethod.Post, "api/events", ToEventRequest(vm));
    var expired = SessionExpired(response);
    if (expired != null) return expired;

    if (!response.IsSuccessStatusCode)
    {
        ModelState.AddModelError("", await _api.ReadErrorAsync(response));
        return View("EventForm", vm);
    }

    // The event needs to exist before a banner can be attached to it
    var created = await _api.ReadAsync<EventDto>(response);
    var bannerProblem = await TryUploadBannerAsync(created?.EventId ?? 0, vm.Banner);

    TempData["Success"] = "Event created.";
    if (bannerProblem != null) TempData["Error"] = bannerProblem;
    return RedirectToAction(nameof(Events));
}

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var (ev, redirect) = await LoadOwnedEventAsync(id);
        if (redirect != null) return redirect;

        return View("EventForm", new EventFormVm
        {
            EventId = ev!.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormVm vm)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        if (!ModelState.IsValid) return View("EventForm", vm);

        var response = await _api.SendAsync(HttpMethod.Put, $"api/events/{vm.EventId}", ToEventRequest(vm));
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await _api.ReadErrorAsync(response));
            return View("EventForm", vm);
        }

        var bannerProblem = await TryUploadBannerAsync(vm.EventId, vm.Banner);

        TempData["Success"] = "Event updated.";
        if (bannerProblem != null) TempData["Error"] = bannerProblem;
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Event deleted.";
        else
            TempData["Error"] = await _api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Events));
    }

    // ================= Categories =================

    public async Task<IActionResult> Categories(int eventId)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var (ev, redirect) = await LoadOwnedEventAsync(eventId);
        if (redirect != null) return redirect;

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/categories");
        var categories = await _api.ReadAsync<List<CategoryDto>>(response) ?? new();

        return View(new CategoriesPageVm { Event = ev!, Categories = categories });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryFormVm vm)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError();
            return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
        }

        var response = await _api.SendAsync(HttpMethod.Post, $"api/events/{vm.EventId}/categories",
            new CreateCategoryRequest
            {
                CategoryName = vm.CategoryName,
                DistanceKm = vm.DistanceKm,
                MaxParticipants = vm.MaxParticipants
            });
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category added.";
        else
            TempData["Error"] = await _api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int eventId, int id)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var (_, redirect) = await LoadOwnedEventAsync(eventId);
        if (redirect != null) return redirect;

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/categories");
        var categories = await _api.ReadAsync<List<CategoryDto>>(response) ?? new();
        var category = categories.FirstOrDefault(c => c.CategoryId == id);

        if (category == null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        return View("CategoryForm", new CategoryFormVm
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormVm vm)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        if (!ModelState.IsValid) return View("CategoryForm", vm);

        var response = await _api.SendAsync(HttpMethod.Put, $"api/categories/{vm.CategoryId}",
            new CreateCategoryRequest
            {
                CategoryName = vm.CategoryName,
                DistanceKm = vm.DistanceKm,
                MaxParticipants = vm.MaxParticipants
            });
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await _api.ReadErrorAsync(response));
            return View("CategoryForm", vm);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category deleted.";
        else
            TempData["Error"] = await _api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ================= Enrolments =================

    public async Task<IActionResult> Enrolments()
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var eventsResponse = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        var expired = SessionExpired(eventsResponse);
        if (expired != null) return expired;

        var events = await _api.ReadAsync<List<EventDto>>(eventsResponse) ?? new();
        var model = new List<EventEnrolmentsVm>();

        foreach (var ev in events)
        {
            var response = await _api.SendAsync(HttpMethod.Get, $"api/enrolments/event/{ev.EventId}");
            var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? new();
            model.Add(new EventEnrolmentsVm { Event = ev, Enrolments = enrolments });
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrolmentStatus(int id, string status)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Put, $"api/enrolments/{id}/status",
            new UpdateEnrolmentStatusRequest { Status = status });
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Enrolment status updated.";
        else
            TempData["Error"] = await _api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Enrolments));
    }

    // ================= Results =================

    public async Task<IActionResult> Results(int? eventId)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        var eventsResponse = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        var expired = SessionExpired(eventsResponse);
        if (expired != null) return expired;

        var events = await _api.ReadAsync<List<EventDto>>(eventsResponse) ?? new();
        var vm = new ResultsPageVm { Events = events };

        if (events.Count == 0) return View(vm);

        var selected = events.FirstOrDefault(e => e.EventId == eventId) ?? events[0];
        vm.SelectedEvent = selected;

        var enrolResponse = await _api.SendAsync(HttpMethod.Get, $"api/enrolments/event/{selected.EventId}");
        var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(enrolResponse) ?? new();
        vm.Enrolments = enrolments.Where(e => e.Status != "Cancelled").ToList();

        var resultResponse = await _api.SendAsync(HttpMethod.Get, $"api/results/event/{selected.EventId}");
        var results = await _api.ReadAsync<List<ResultDto>>(resultResponse) ?? new();
        vm.ResultsByEnrolment = results.ToDictionary(r => r.EnrolmentId);

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResult(
        int eventId, int enrolmentId, int hours, int minutes, int seconds, int position)
    {
        var guard = RequireRole("Organiser");
        if (guard != null) return guard;

        if (hours < 0 || minutes < 0 || seconds < 0)
        {
            TempData["Error"] = "Time values cannot be negative.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var totalSeconds = (hours * 3600) + (minutes * 60) + seconds;

        if (totalSeconds <= 0 || position < 1)
        {
            TempData["Error"] = "Enter a finish time and a finishing position of 1 or more.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var response = await _api.SendAsync(HttpMethod.Post, "api/results", new CreateResultRequest
        {
            EnrolmentId = enrolmentId,
            FinishTimeSeconds = totalSeconds,
            FinishPosition = position
        });
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Result saved.";
        else
            TempData["Error"] = await _api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ================= Helpers =================

    /// <summary>Loads an event and confirms the logged-in Organiser owns it.</summary>
    private async Task<(EventDto? Event, IActionResult? Redirect)> LoadOwnedEventAsync(int eventId)
    {
        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}");
        var expired = SessionExpired(response);
        if (expired != null) return (null, expired);

        var ev = await _api.ReadAsync<EventDto>(response);
        if (ev == null)
        {
            TempData["Error"] = "That event could not be found.";
            return (null, RedirectToAction(nameof(Events)));
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = "You can only manage your own events.";
            return (null, RedirectToAction(nameof(Events)));
        }

        return (ev, null);

    }
    /// <summary>Sends the banner to the API. Returns a message if it failed, otherwise null.</summary>
    private async Task<string?> TryUploadBannerAsync(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0 || eventId == 0) return null;

        var response = await _api.UploadFileAsync($"api/events/{eventId}/banner", banner);
        if (response.IsSuccessStatusCode) return null;

        return "The event was saved, but the banner could not be uploaded: "
               + await _api.ReadErrorAsync(response);
    }

    private static CreateEventRequest ToEventRequest(EventFormVm vm) => new()
    {
        EventName = vm.EventName,
        Description = vm.Description,
        EventDate = vm.EventDate,
        Location = vm.Location,
        Distance = vm.Distance,
        EventType = vm.EventType
    };

    private string FirstModelError() =>
        ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
        ?? "Please check the details and try again.";
}