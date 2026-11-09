using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class EventManagementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventManagementTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.SampleEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var request = TestHelpers.SampleEvent();
        request.EventType = "Swimming";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_AsParticipant_Returns200()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_Missing_Returns404()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_OwnEvent_Returns200AndSavesChange()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var update = new UpdateEventRequest
        {
            EventName = "Renamed Fun Run",
            Description = created.Description,
            EventDate = created.EventDate,
            Location = created.Location,
            Distance = created.Distance,
            EventType = created.EventType
        };

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Renamed Fun Run", body!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_AnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{created.EventId}",
            new UpdateEventRequest
            {
                EventName = "Hijacked",
                EventDate = created.EventDate,
                Location = created.Location,
                Distance = created.Distance,
                EventType = created.EventType
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_OwnEvent_Returns200ThenEventIsGone()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        var afterDelete = await organiser.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await otherOrganiser.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}