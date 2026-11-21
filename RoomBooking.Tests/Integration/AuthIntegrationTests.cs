using System.Net;
using System.Net.Http.Json;
using RoomBooking.Application.Auth;
using Xunit;

namespace RoomBooking.Tests.Integration;

[Collection("Api")]
public class AuthIntegrationTests
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task Register_WithValidData_Returns201AndNeverExposesThePasswordHash()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = NewEmail(),
            Password = ApiHelpers.UserPassword,
            FullName = "Alice"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordhash", raw, StringComparison.OrdinalIgnoreCase);

        var user = System.Text.Json.JsonSerializer.Deserialize<UserResponse>(
            raw, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal("User", user!.Role);
    }

    [Fact]
    public async Task Register_WithTheSameEmailInDifferentCase_Returns409()
    {
        var email = NewEmail();
        var first = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.UserPassword,
            FullName = "First"
        });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email.ToUpperInvariant(),
            Password = ApiHelpers.UserPassword,
            FullName = "Second"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WhenTheBodyAsksForTheAdminRole_StillCreatesARegularUser()
    {
        // The request type has no "role" field, so the extra property must be ignored.
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = NewEmail(),
            password = ApiHelpers.UserPassword,
            fullName = "Sneaky",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("User", user!.Role);
    }

    [Fact]
    public async Task Register_WithInvalidData_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = "not-an-email",
            Password = "short",
            FullName = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithAWrongPassword_Returns401()
    {
        var email = NewEmail();
        (await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.UserPassword,
            FullName = "Alice"
        })).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithAnUnknownEmail_Returns401WithTheSameMessageAsAWrongPassword()
    {
        var email = NewEmail();
        (await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.UserPassword,
            FullName = "Alice"
        })).EnsureSuccessStatusCode();

        var wrongPassword = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "wrong-password"
        });
        var unknownEmail = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = NewEmail(),
            Password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        // Identical messages mean the API does not reveal which emails are registered.
        Assert.Equal(
            await ApiHelpers.ReadDetailAsync(wrongPassword),
            await ApiHelpers.ReadDetailAsync(unknownEmail));
    }

    [Fact]
    public async Task AnyProtectedEndpoint_WithAGarbageToken_Returns401()
    {
        var response = await ApiHelpers.SendAsync(
            _client, HttpMethod.Get, "/api/rooms", token: "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}