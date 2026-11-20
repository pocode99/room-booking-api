namespace RoomBooking.Application.Bookings;

public record BookingResponse(
    int Id,
    int RoomId,
    string RoomName,
    int UserId,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Status,
    DateTimeOffset CreatedAtUtc);