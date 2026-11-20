using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Bookings;

public interface IBookingRepository
{
    // Returns the booking with its Room loaded. The entity is tracked, so it can be modified.
    Task<Booking?> GetByIdAsync(int id, CancellationToken ct);

    // True if an ACTIVE booking in the same room overlaps the given time range.
    // Cancelled bookings never block a new booking.
    Task<bool> HasOverlapAsync(int roomId, DateTime startUtc, DateTime endUtc, CancellationToken ct);

    Task AddAsync(Booking booking, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}