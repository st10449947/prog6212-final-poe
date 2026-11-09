using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class RoleEnforcementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleEnforcementTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.SampleEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{created.EventId}/categories",
            new CreateCategoryRequest { CategoryName = "Sneaky", DistanceKm = 5, MaxParticipants = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);

        var response = await organiser.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = created.EventId, CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewEventEnrolments_AsParticipant_Returns403()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/enrolments/event/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_AsParticipant_Returns403()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = 1, FinishTimeSeconds = 3600, FinishPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewMyResults_AsOrganiser_Returns403()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");

        var response = await organiser.GetAsync("/api/results/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}