using System.Net;
using System.Net.Http.Headers;

namespace RaceDay.Api.Tests;

public class UploadSecurityTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public UploadSecurityTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent FileForm(string fileName, string contentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 1, 2, 3 });
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task UploadBanner_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/events/1/banner", FileForm("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_Returns403()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsync($"/api/events/{created.EventId}/banner",
            FileForm("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsDifferentOrganiser_Returns403()
    {
        var owner = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(owner);

        var response = await otherOrganiser.PostAsync($"/api/events/{created.EventId}/banner",
            FileForm("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_NotAnImage_Returns400()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{created.EventId}/banner",
            FileForm("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfileImage_NotAnImage_Returns400()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");

        var response = await participant.PostAsync("/api/profile/me/image",
            FileForm("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}