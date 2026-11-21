using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RoomBooking.Application.Auth;
using RoomBooking.Application.Rooms;
using Xunit;

namespace RoomBooking.Tests.Integration;

[Collection("Api")]
public class SmokeTests
{
    private readonly HttpClient _client;

    public SmokeTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRooms_WithoutAToken_Returns401()
    {
        var response = await _client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRooms_AsTheSeededAdmin_ReturnsTheFourSeededRooms()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = ApiFactory.AdminEmail,
            Password = ApiFactory.AdminPassword
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/rooms");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomResponse>>();
        Assert.Equal(4, rooms!.Count);
    }
}