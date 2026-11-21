using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.Bookings;

public record BookingSearchRequest
{
    public int? RoomId { get; init; }

    // Returns bookings that end after this moment.
    public DateTimeOffset? From { get; init; }

    // Returns bookings that start before this moment.
    public DateTimeOffset? To { get; init; }

    // The upper bound keeps (Page - 1) * PageSize safely inside the int range.
    [Range(1, 100_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}