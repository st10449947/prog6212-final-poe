using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>Starts the API for tests, using an in-memory database instead of SQL Server.</summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration made in Program.cs
            var efServices = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericArguments().Contains(typeof(RaceDayDbContext)))
                .ToList();
            foreach (var d in efServices) services.Remove(d);

            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class TestHelpers
{
    public const string Password = "Password123";

    /// <summary>Registers a brand-new user, logs in, and returns a client holding that session.</summary>
    public static async Task<HttpClient> LoginAsNewUserAsync(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = Password,
            Role = role
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();

        return client;
    }

    public static CreateEventRequest SampleEvent() => new()
    {
        EventName = "Joburg City Fun Run",
        Description = "A community road run.",
        EventDate = DateTime.Today.AddMonths(2),
        Location = "Johannesburg, Gauteng",
        Distance = 21.1m,
        EventType = "Running"
    };

    public static async Task<EventDto> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", SampleEvent());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest { CategoryName = "10km Fun Run", DistanceKm = 10, MaxParticipants = 100 });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = eventId, CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}