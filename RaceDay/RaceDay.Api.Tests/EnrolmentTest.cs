using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = created.EventId, CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal(category.CategoryId, body!.CategoryId);
    }

    [Fact]
    public async Task Enrol_SameEventTwice_Returns409()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);
        await TestHelpers.EnrolAsync(participant, created.EventId, category.CategoryId);

        var second = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = created.EventId, CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryFromDifferentEvent_Returns400()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var eventA = await TestHelpers.CreateEventAsync(organiser);
        var eventB = await TestHelpers.CreateEventAsync(organiser);
        var categoryOfB = await TestHelpers.CreateCategoryAsync(organiser, eventB.EventId);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = eventA.EventId, CategoryId = categoryOfB.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_MissingEvent_Returns404()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = 999999, CategoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var aisha = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var johan = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);
        await TestHelpers.EnrolAsync(aisha, created.EventId, category.CategoryId);

        var aishaList = await aisha.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");
        var johanList = await johan.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");

        Assert.Single(aishaList!);
        Assert.Empty(johanList!);
    }

    [Fact]
    public async Task ViewEventEnrolments_AsOwningOrganiser_Returns200()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);
        await TestHelpers.EnrolAsync(participant, created.EventId, category.CategoryId);

        var response = await organiser.GetAsync($"/api/enrolments/event/{created.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();
        Assert.Single(list!);
    }

    [Fact]
    public async Task ViewEventEnrolments_AsDifferentOrganiser_Returns403()
    {
        var owner = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await otherOrganiser.GetAsync($"/api/enrolments/event/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_OrganiserRecordsResult_ParticipantSeesIt()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestHelpers.EnrolAsync(participant, created.EventId, category.CategoryId);

        var record = await organiser.PostAsJsonAsync("/api/results", new CreateResultRequest
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTimeSeconds = 3600,
            FinishPosition = 1
        });
        var myResults = await participant.GetFromJsonAsync<List<ResultDto>>("/api/results/mine");

        Assert.Equal(HttpStatusCode.Created, record.StatusCode);
        Assert.Single(myResults!);
        Assert.Equal(1, myResults![0].FinishPosition);
    }
}