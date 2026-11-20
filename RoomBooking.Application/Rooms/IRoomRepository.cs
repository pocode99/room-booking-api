using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Rooms;

public interface IRoomRepository
{
    Task<List<Room>> GetAllAsync(CancellationToken ct);

    Task<Room?> GetByIdAsync(int id, CancellationToken ct);

    // Checks whether another room already uses this name.
    // excludeId lets an update ignore the room being edited.
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);

    // True if the room has any active booking that has not ended yet.
    Task<bool> HasUpcomingBookingsAsync(int roomId, DateTime utcNow, CancellationToken ct);

    Task AddAsync(Room room, CancellationToken ct);

    void Remove(Room room);

    Task SaveChangesAsync(CancellationToken ct);
}