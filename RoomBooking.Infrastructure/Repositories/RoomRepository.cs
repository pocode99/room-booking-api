using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Rooms;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Infrastructure.Persistence;

namespace RoomBooking.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;

    public RoomRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Room>> GetAllAsync(CancellationToken ct) =>
        _db.Rooms
            .AsNoTracking()   // read-only query, no change tracking needed
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    // Tracked on purpose: the service may update or delete the returned entity.
    public Task<Room?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    // Case-insensitive check, because the SQLite unique index is case-sensitive.
    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        var lowered = name.ToLower();
        return _db.Rooms.AnyAsync(
            r => r.Name.ToLower() == lowered && (excludeId == null || r.Id != excludeId),
            ct);
    }

    public Task<bool> HasUpcomingBookingsAsync(int roomId, DateTime utcNow, CancellationToken ct) =>
        _db.Bookings.AnyAsync(
            b => b.RoomId == roomId
                 && b.Status == BookingStatus.Active
                 && b.EndUtc > utcNow,
            ct);

    public async Task AddAsync(Room room, CancellationToken ct) =>
        await _db.Rooms.AddAsync(room, ct);

    public void Remove(Room room) => _db.Rooms.Remove(room);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}