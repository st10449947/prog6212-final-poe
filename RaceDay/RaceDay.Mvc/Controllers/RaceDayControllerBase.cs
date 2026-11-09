using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public abstract class RaceDayControllerBase : Controller
{
    protected bool IsLoggedIn => HttpContext.Session.GetInt32("UserId").HasValue;
    protected int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;
    protected string Role => HttpContext.Session.GetString("Role") ?? string.Empty;

    /// <summary>
    /// Returns a redirect if this user may not see the page, otherwise null.
    /// Not logged in -> Login page. Wrong role -> Access Denied page.
    /// </summary>
    protected IActionResult? RequireRole(string role)
    {
        if (!IsLoggedIn)
            return RedirectToAction("Login", "Auth");

        if (!string.Equals(Role, role, StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("AccessDenied", "Home");

        return null;
    }

    /// <summary>
    /// Call this on the first API response of a page. It handles two situations:
    /// the API is switched off (friendly message, back to the home page), and the API
    /// no longer remembers the login (401: clear the website session, back to Login).
    /// </summary>
    protected IActionResult? SessionExpired(HttpResponseMessage response)
    {
        if (RaceDayApiClient.IsUnreachable(response))
        {
            TempData["Error"] = RaceDayApiClient.UnreachableMessage;
            return RedirectToAction("Index", "Home");
        }

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return null;

        HttpContext.Session.Clear();
        TempData["Error"] = "Your session expired. Please log in again.";
        return RedirectToAction("Login", "Auth");
    }
}