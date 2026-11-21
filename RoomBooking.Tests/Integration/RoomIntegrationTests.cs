using System.Net;
using Xunit;

namespace RoomBooking.Tests.Integration;

[Collection("Api")]
public class RoomIntegrationTests
{
    private readonly HttpClient _client;

    public RoomIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRoom_AsAdmin_Returns201AndALocationHeader()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);

        var response = await ApiHelpers.SendAsync(_client, HttpMethod.Post, "/api/rooms", admin,
            new { name = $"Room-{Guid.NewGuid():N}", capacity = 8 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateRoom_AsARegularUser_Returns403()
    {
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.SendAsync(_client, HttpMethod.Post, "/api/rooms", user,
            new { name = $"Room-{Guid.NewGuid():N}", capacity = 8 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WithTheSameNameInDifferentCase_Returns409()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);
        var room = await ApiHelpers.CreateRoomAsync(_client, admin);

        var response = await ApiHelpers.SendAsync(_client, HttpMethod.Post, "/api/rooms", admin,
            new { name = room.Name.ToUpperInvariant(), capacity = 5 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WithInvalidData_Returns400()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);

        var response = await ApiHelpers.SendAsync(_client, HttpMethod.Post, "/api/rooms", admin,
            new { name = "", capacity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRoom_ThatDoesNotExist_Returns404()
    {
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.SendAsync(_client, HttpMethod.Get, "/api/rooms/999999", user);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRoom_WithAnUpcomingBooking_Returns409()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);
        var room = await ApiHelpers.CreateRoomAsync(_client, admin);
        await ApiHelpers.BookOrFailAsync(
            _client, admin, room.Id, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var response = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/rooms/{room.Id}", admin);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("upcoming bookings", await ApiHelpers.ReadDetailAsync(response));
    }

    [Fact]
    public async Task DeleteRoom_WithNoBookings_Returns204AndTheRoomIsGone()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);
        var room = await ApiHelpers.CreateRoomAsync(_client, admin);

        var delete = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/rooms/{room.Id}", admin);
        var get = await ApiHelpers.SendAsync(
            _client, HttpMethod.Get, $"/api/rooms/{room.Id}", admin);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    // Documents a known limitation: the foreign key blocks deleting a room that still has
    // cancelled (or past) bookings, even though the service only checks upcoming ones.
    [Fact]
    public async Task DeleteRoom_WhenOnlyACancelledBookingExists_Returns409BecauseOfTheForeignKey()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);
        var room = await ApiHelpers.CreateRoomAsync(_client, admin);
        var booking = await ApiHelpers.BookOrFailAsync(
            _client, admin, room.Id, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var cancel = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/bookings/{booking.Id}", admin);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var response = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/rooms/{room.Id}", admin);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}