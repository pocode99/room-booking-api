using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Bookings;

public interface IBookingRepository
{
    // Returns the booking with its Room loaded. The entity is tracked, so it can be modified.
    Task<Booking?> GetByIdAsync(int id, CancellationToken ct);

    // Atomically checks that no ACTIVE booking in the same room overlaps the given range
    // and, if the room is free, saves the booking. Returns false when the room is taken.
    // The check and the insert share one transaction, so two concurrent requests
    // can never both succeed for the same time slot.
    Task<bool> TryAddIfAvailableAsync(Booking booking, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}