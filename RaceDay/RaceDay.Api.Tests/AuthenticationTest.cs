using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class AuthenticationTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthenticationTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static RegisterRequest NewRegistration(string role = "Participant") => new()
    {
        FullName = "Aisha Patel",
        Email = $"user_{Guid.NewGuid():N}@test.com",
        Password = TestHelpers.Password,
        Role = role
    };

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Participant", body!.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        var second = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        request.Password = "short";

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == request.Email);

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual(request.Password, user.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = request.Email, Password = request.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = request.Email, Password = "WrongPassword9" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@test.com", Password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_AfterLogin_ClearsSession()
    {
        var client = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}