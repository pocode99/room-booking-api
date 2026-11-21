using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RoomBooking.Application.Auth;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Rooms;

namespace RoomBooking.Tests.Integration;

internal static class ApiHelpers
{
    public const string UserPassword = "Passw0rd!123";

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    public static Task<string> LoginAdminAsync(HttpClient client) =>
        LoginAsync(client, ApiFactory.AdminEmail, ApiFactory.AdminPassword);

    // Registers a brand new user with a random email and returns a token for that user.
    public static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = UserPassword,
            FullName = "Test User"
        });
        register.EnsureSuccessStatusCode();

        return await LoginAsync(client, email, UserPassword);
    }

    public static Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string url, string? token = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return client.SendAsync(request);
    }

    // Every test works in its own room, so time slots never collide between tests.
    public static async Task<RoomResponse> CreateRoomAsync(
        HttpClient client, string adminToken, string? name = null)
    {
        var response = await SendAsync(client, HttpMethod.Post, "/api/rooms", adminToken,
            new CreateRoomRequest { Name = name ?? $"Room-{Guid.NewGuid():N}", Capacity = 10 });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoomResponse>())!;
    }

    public static Task<HttpResponseMessage> BookAsync(
        HttpClient client, string token, int roomId,
        DateTimeOffset start, DateTimeOffset end, string title = "Test booking") =>
        SendAsync(client, HttpMethod.Post, "/api/bookings", token,
            new CreateBookingRequest { RoomId = roomId, Title = title, Start = start, End = end });

    // Books a slot that is expected to be free and returns the created booking.
    public static async Task<BookingResponse> BookOrFailAsync(
        HttpClient client, string token, int roomId,
        DateTimeOffset start, DateTimeOffset end, string title = "Test booking")
    {
        var response = await BookAsync(client, token, roomId, start, end, title);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BookingResponse>())!;
    }

    // A time well in the future, so the "cannot start in the past" rule never gets in the way.
    public static DateTimeOffset FutureAt(int hour, int minute = 0, int daysAhead = 30)
    {
        var day = DateTime.UtcNow.Date.AddDays(daysAhead);
        return new DateTimeOffset(day.AddHours(hour).AddMinutes(minute), TimeSpan.Zero);
    }

    // Reads the "detail" field of a problem details response.
    public static async Task<string> ReadDetailAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("detail").GetString() ?? "";
    }
}