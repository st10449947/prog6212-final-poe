using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class AuthController : Controller
{
    private readonly RaceDayApiClient _api;

    public AuthController(RaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/register", new RegisterRequest
        {
            FullName = vm.FullName,
            Email = vm.Email,
            Password = vm.Password,
            Role = vm.Role
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await _api.ReadErrorAsync(response));
            return View(vm);
        }

        TempData["Success"] = "Registration successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/login",
            new LoginRequest { Email = vm.Email, Password = vm.Password });

        if (RaceDayApiClient.IsUnreachable(response))
        {
            ModelState.AddModelError("", RaceDayApiClient.UnreachableMessage);
            return View(vm);
        }

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }

        var auth = await _api.ReadAsync<AuthResponse>(response);
        if (auth == null)
        {
            ModelState.AddModelError("", "Login succeeded but the reply could not be read.");
            return View(vm);
        }

        // Keep the API's session cookie so later API calls are recognised as this user
        var apiCookie = RaceDayApiClient.ExtractSessionCookie(response);
        if (!string.IsNullOrWhiteSpace(apiCookie))
            HttpContext.Session.SetString("ApiSessionCookie", apiCookie);

        // The MVC site's own session: who is logged in and what role they have
        HttpContext.Session.SetInt32("UserId", auth.UserId);
        HttpContext.Session.SetString("Role", auth.Role);
        HttpContext.Session.SetString("DisplayName", auth.FullName);

        return auth.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Participant");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.SendAsync(HttpMethod.Post, "api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}