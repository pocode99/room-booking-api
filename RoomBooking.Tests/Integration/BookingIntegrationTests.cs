using System.Net;
using System.Net.Http.Json;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Common;
using Xunit;

namespace RoomBooking.Tests.Integration;

[Collection("Api")]
public class BookingIntegrationTests
{
    private readonly HttpClient _client;

    public BookingIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Creates a fresh room and returns its id together with an admin token.
    private async Task<(int RoomId, string AdminToken)> NewRoomAsync()
    {
        var admin = await ApiHelpers.LoginAdminAsync(_client);
        var room = await ApiHelpers.CreateRoomAsync(_client, admin);
        return (room.Id, admin);
    }

    [Fact]
    public async Task Book_WithValidData_Returns201AndTheBookingCanBeReadBack()
    {
        var (roomId, _) = await NewRoomAsync();
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.BookAsync(
            _client, user, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal("Active", created!.Status);
        Assert.Equal(roomId, created.RoomId);
        Assert.Equal(ApiHelpers.FutureAt(9), created.StartUtc);

        var read = await ApiHelpers.SendAsync(
            _client, HttpMethod.Get, $"/api/bookings/{created.Id}", user);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task Book_WhenTheSlotIsTaken_Returns409WithProblemDetails()
    {
        var (roomId, _) = await NewRoomAsync();
        var alice = await ApiHelpers.RegisterAndLoginAsync(_client);
        var bob = await ApiHelpers.RegisterAndLoginAsync(_client);
        await ApiHelpers.BookOrFailAsync(
            _client, alice, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var response = await ApiHelpers.BookAsync(
            _client, bob, roomId, ApiHelpers.FutureAt(9, 30), ApiHelpers.FutureAt(10, 30));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("already booked", await ApiHelpers.ReadDetailAsync(response));
    }

    [Fact]
    public async Task Book_BackToBack_Succeeds()
    {
        var (roomId, _) = await NewRoomAsync();
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);
        await ApiHelpers.BookOrFailAsync(
            _client, user, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var response = await ApiHelpers.BookAsync(
            _client, user, roomId, ApiHelpers.FutureAt(10), ApiHelpers.FutureAt(11));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Book_WhenEndIsBeforeStart_Returns400()
    {
        var (roomId, _) = await NewRoomAsync();
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.BookAsync(
            _client, user, roomId, ApiHelpers.FutureAt(11), ApiHelpers.FutureAt(9));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_WhenLongerThanFourHours_Returns400()
    {
        var (roomId, _) = await NewRoomAsync();
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.BookAsync(
            _client, user, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(14));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_InThePast_Returns400()
    {
        var (roomId, _) = await NewRoomAsync();
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.BookAsync(
            _client, user, roomId,
            ApiHelpers.FutureAt(9, daysAhead: -1), ApiHelpers.FutureAt(10, daysAhead: -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_OnAMissingRoom_Returns404()
    {
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        var response = await ApiHelpers.BookAsync(
            _client, user, roomId: 999999, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Book_WithoutAToken_Returns401()
    {
        var (roomId, _) = await NewRoomAsync();

        var response = await ApiHelpers.SendAsync(_client, HttpMethod.Post, "/api/bookings", token: null,
            body: new CreateBookingRequest
            {
                RoomId = roomId,
                Title = "No token",
                Start = ApiHelpers.FutureAt(9),
                End = ApiHelpers.FutureAt(10)
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ByTheOwner_Returns204AndTheSlotCanBeBookedAgain()
    {
        var (roomId, _) = await NewRoomAsync();
        var owner = await ApiHelpers.RegisterAndLoginAsync(_client);
        var booking = await ApiHelpers.BookOrFailAsync(
            _client, owner, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var cancel = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/bookings/{booking.Id}", owner);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var read = await ApiHelpers.SendAsync(
            _client, HttpMethod.Get, $"/api/bookings/{booking.Id}", owner);
        var cancelled = await read.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal("Cancelled", cancelled!.Status);

        // A cancelled booking must not block the same slot.
        var rebook = await ApiHelpers.BookAsync(
            _client, owner, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));
        Assert.Equal(HttpStatusCode.Created, rebook.StatusCode);
    }

    [Fact]
    public async Task Cancel_ByAnotherUser_Returns403AndTheBookingStaysActive()
    {
        var (roomId, _) = await NewRoomAsync();
        var owner = await ApiHelpers.RegisterAndLoginAsync(_client);
        var stranger = await ApiHelpers.RegisterAndLoginAsync(_client);
        var booking = await ApiHelpers.BookOrFailAsync(
            _client, owner, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var cancel = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/bookings/{booking.Id}", stranger);

        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);

        var read = await ApiHelpers.SendAsync(
            _client, HttpMethod.Get, $"/api/bookings/{booking.Id}", owner);
        var unchanged = await read.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal("Active", unchanged!.Status);
    }

    [Fact]
    public async Task Cancel_ByAnAdmin_Returns204()
    {
        var (roomId, admin) = await NewRoomAsync();
        var owner = await ApiHelpers.RegisterAndLoginAsync(_client);
        var booking = await ApiHelpers.BookOrFailAsync(
            _client, owner, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10));

        var cancel = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/bookings/{booking.Id}", admin);

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsActiveBookingsInStartOrder_WithPaging()
    {
        var (roomId, _) = await NewRoomAsync();
        var user = await ApiHelpers.RegisterAndLoginAsync(_client);

        // Created out of order on purpose, to prove the results are sorted by start time.
        var b = await ApiHelpers.BookOrFailAsync(
            _client, user, roomId, ApiHelpers.FutureAt(10), ApiHelpers.FutureAt(11), "B");
        await ApiHelpers.BookOrFailAsync(
            _client, user, roomId, ApiHelpers.FutureAt(9), ApiHelpers.FutureAt(10), "A");
        await ApiHelpers.BookOrFailAsync(
            _client, user, roomId, ApiHelpers.FutureAt(11), ApiHelpers.FutureAt(12), "C");

        var page1 = await SearchAsync(user, $"roomId={roomId}&page=1&pageSize=2");
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(new[] { "A", "B" }, page1.Items.Select(i => i.Title));

        var page2 = await SearchAsync(user, $"roomId={roomId}&page=2&pageSize=2");
        Assert.Equal(new[] { "C" }, page2.Items.Select(i => i.Title));

        // Cancelled bookings disappear from the search results.
        var cancel = await ApiHelpers.SendAsync(
            _client, HttpMethod.Delete, $"/api/bookings/{b.Id}", user);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var afterCancel = await SearchAsync(user, $"roomId={roomId}");
        Assert.Equal(2, afterCancel.TotalCount);
        Assert.Equal(new[] { "A", "C" }, afterCancel.Items.Select(i => i.Title));
    }

    private async Task<PagedResult<BookingResponse>> SearchAsync(string token, string query)
    {
        var response = await ApiHelpers.SendAsync(
            _client, HttpMethod.Get, $"/api/bookings?{query}", token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<BookingResponse>>())!;
    }
}