using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Bookings;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Infrastructure.Persistence;

namespace RoomBooking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

    // Tracked on purpose: the service may cancel the returned booking.
    // Room is loaded because the response needs the room name.
    public Task<Booking?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    // Same rule as Booking.Overlaps, written as a query so the database evaluates it.
    // Back-to-back bookings (end == start) are not overlapping because the comparisons are strict.
    public Task<bool> HasOverlapAsync(
        int roomId, DateTime startUtc, DateTime endUtc, CancellationToken ct) =>
        _db.Bookings.AnyAsync(
            b => b.RoomId == roomId
                 && b.Status == BookingStatus.Active
                 && b.StartUtc < endUtc
                 && b.EndUtc > startUtc,
            ct);

    public async Task AddAsync(Booking booking, CancellationToken ct) =>
        await _db.Bookings.AddAsync(booking, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}