using RoomBooking.Domain.Enums;

namespace RoomBooking.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = "";
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;
    public DateTime CreatedAtUtc { get; set; }

    // Two ranges overlap when each one starts before the other ends.
    // Back-to-back bookings (end == start) do not overlap.
    public bool Overlaps(DateTime startUtc, DateTime endUtc)
        => StartUtc < endUtc && EndUtc > startUtc;

    public void Cancel() => Status = BookingStatus.Cancelled;
}