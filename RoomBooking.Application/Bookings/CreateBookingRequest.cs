using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.Bookings;

public record CreateBookingRequest
{
    [Range(1, int.MaxValue)]
    public int RoomId { get; init; }

    [Required]
    [StringLength(200)]
    public string Title { get; init; } = "";

    // Clients should send times with "Z" or an explicit offset, e.g. 2026-12-01T09:00:00Z.
    // The service converts them to UTC before validating and storing.
    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }
}