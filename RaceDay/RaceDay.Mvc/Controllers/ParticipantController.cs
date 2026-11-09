using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class ParticipantController : RaceDayControllerBase
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api)
    {
        _api = api;
    }

    // ================= Upcoming events (with filtering) =================

    public async Task<IActionResult> Index(string? type, string? search)
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/events");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        var events = await _api.ReadAsync<List<EventDto>>(response) ?? new();

        // Only events that have not happened yet
        events = events.Where(e => e.EventDate.Date >= DateTime.Today).ToList();

        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType == type).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            events = events.Where(e =>
                e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(new ParticipantEventsVm
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            Type = type,
            Search = search
        });
    }

    // ================= Event details + choose a category =================

    public async Task<IActionResult> Details(int id)
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        var eventResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        var expired = SessionExpired(eventResponse);
        if (expired != null) return expired;

        var ev = await _api.ReadAsync<EventDto>(eventResponse);
        if (ev == null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var categoryResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}/categories");
        var categories = await _api.ReadAsync<List<CategoryDto>>(categoryResponse) ?? new();

        // Has this Participant already entered this event?
        var mineResponse = await _api.SendAsync(HttpMethod.Get, "api/enrolments/mine");
        var mine = await _api.ReadAsync<List<EnrolmentDto>>(mineResponse) ?? new();

        return View(new EventDetailsVm
        {
            Event = ev,
            Categories = categories.OrderBy(c => c.DistanceKm).ToList(),
            ExistingEnrolment = mine.FirstOrDefault(e => e.EventId == id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category first.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var response = await _api.SendAsync(HttpMethod.Post, "api/enrolments",
            new CreateEnrolmentRequest { EventId = eventId, CategoryId = categoryId });
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await _api.ReadErrorAsync(response);
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You have entered the event.";
        return RedirectToAction(nameof(MyEvents));
    }

    // ================= My Events =================

    public async Task<IActionResult> MyEvents()
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/enrolments/mine");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? new();
        return View(enrolments.OrderBy(e => e.EventDate).ToList());
    }

    // ================= My Results =================

    public async Task<IActionResult> MyResults()
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/results/mine");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        var results = await _api.ReadAsync<List<ResultDto>>(response) ?? new();
        return View(results.OrderByDescending(r => r.EventDate).ToList());
    }

    // ================= Profile =================

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/profile/me");
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        var profile = await _api.ReadAsync<ProfileDto>(response);
        if (profile == null)
        {
            TempData["Error"] = "Your profile could not be loaded.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ProfileFormVm
        {
            FullName = profile.FullName,
            Email = profile.Email,
            Role = profile.Role,
            MemberSince = profile.CreatedAt,
            ProfileImageUrl = profile.ProfileImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileFormVm vm)
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        if (!ModelState.IsValid)
        {
            await FillReadOnlyFieldsAsync(vm);
            return View(vm);
        }

        var response = await _api.SendAsync(HttpMethod.Put, "api/profile/me",
            new UpdateProfileRequest { FullName = vm.FullName, Email = vm.Email });
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await _api.ReadErrorAsync(response));
            await FillReadOnlyFieldsAsync(vm);
            return View(vm);
        }

        // Keep the name in the top bar up to date
        var updated = await _api.ReadAsync<ProfileDto>(response);
        if (updated != null)
            HttpContext.Session.SetString("DisplayName", updated.FullName);

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfileImage(IFormFile? image)
    {
        var guard = RequireRole("Participant");
        if (guard != null) return guard;

        if (image == null || image.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Profile));
        }

        var response = await _api.UploadFileAsync("api/profile/me/image", image);
        var expired = SessionExpired(response);
        if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Profile picture updated.";
        else
            TempData["Error"] = await _api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Profile));
    }

    // The form only posts name and email, so reload the display-only fields when redisplaying it
    private async Task FillReadOnlyFieldsAsync(ProfileFormVm vm)
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/profile/me");
        var profile = await _api.ReadAsync<ProfileDto>(response);
        if (profile == null) return;

        vm.Role = profile.Role;
        vm.MemberSince = profile.CreatedAt;
        vm.ProfileImageUrl = profile.ProfileImageUrl;
    }
}