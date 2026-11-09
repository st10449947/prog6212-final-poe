using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters;

/// <summary>
/// [SessionAuth] = any logged-in user. [SessionAuth("Organiser")] = that role only.
/// Returns 401 when not logged in and 403 when the role is wrong.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public SessionAuthAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Please log in first." });
            return;
        }

        if (_allowedRoles.Length > 0 && (role == null || !_allowedRoles.Contains(role)))
        {
            context.Result = new ObjectResult(new { message = "You do not have permission to do this." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

public static class SessionUserExtensions
{
    public static int GetUserId(this ISession session) => session.GetInt32("UserId") ?? 0;
    public static string GetRole(this ISession session) => session.GetString("Role") ?? string.Empty;
}