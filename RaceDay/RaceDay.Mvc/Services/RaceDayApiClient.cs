using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Every call from the MVC site to the RaceDay API goes through this class.
/// It forwards the API session cookie so the API knows who is logged in.
/// </summary>
public class RaceDayApiClient
{
    private const string UnreachableHeader = "X-RaceDay-Unreachable";
    public const string UnreachableMessage = "RaceDay could not reach its service. Please try again in a moment.";

    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _accessor;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RaceDayApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor)
    {
        _factory = factory;
        _accessor = accessor;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        // Attach the API session cookie saved at login
        var apiCookie = _accessor.HttpContext?.Session.GetString("ApiSessionCookie");
        if (!string.IsNullOrWhiteSpace(apiCookie))
            request.Headers.TryAddWithoutValidation("Cookie", apiCookie);

        return request;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var client = _factory.CreateClient("RaceDayApi");
        using var request = CreateRequest(method, url);

        if (body != null)
            request.Content = JsonContent.Create(body);

        try
        {
            return await client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Unreachable();   // the API is switched off or cannot be reached
        }
    }

    /// <summary>Sends a file to the API as multipart form data. The API stores it in Azure.</summary>
    public async Task<HttpResponseMessage> UploadFileAsync(string url, IFormFile file)
    {
        var client = _factory.CreateClient("RaceDayApi");
        using var request = CreateRequest(HttpMethod.Post, url);
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();

        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

        form.Add(fileContent, "file", file.FileName);   // "file" must match the API's parameter name
        request.Content = form;

        try
        {
            return await client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Unreachable();
        }
    }

    public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>Turns a failed API response into a short message that is safe to show a user.</summary>
    public async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        if (IsUnreachable(response))
            return UnreachableMessage;

        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Our controllers return { "message": "..." }
                if (doc.RootElement.TryGetProperty("message", out var message) &&
                    !string.IsNullOrWhiteSpace(message.GetString()))
                    return message.GetString()!;

                // Automatic validation errors look like { "errors": { "Field": ["..."] } }
                if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Object)
                {
                    var first = errors.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Select(e => e.GetString())
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    if (first != null) return first;
                }
            }
        }
        catch
        {
            // Not JSON: fall through to the friendly default below
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "That item could not be found.",
            _ => "Something went wrong. Please try again."
        };
    }

    /// <summary>Pulls "name=value" out of the API's Set-Cookie header after login.</summary>
    public static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return null;

        var header = values.FirstOrDefault(v =>
            v.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));

        // A request's Cookie header only needs name=value, not Path or HttpOnly
        return header?.Split(';', 2)[0];
    }

    // A made-up 503 that carries a marker, so we can tell "API is off" from a real API error
    private static HttpResponseMessage Unreachable()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryAddWithoutValidation(UnreachableHeader, "true");
        return response;
    }

    public static bool IsUnreachable(HttpResponseMessage response) =>
        response.Headers.Contains(UnreachableHeader);
}