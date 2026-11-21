using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Exceptions;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Infrastructure.Persistence;

namespace RoomBooking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    // SQLite result codes: 5 = SQLITE_BUSY, 6 = SQLITE_LOCKED.
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

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

    public async Task<bool> TryAddIfAvailableAsync(Booking booking, CancellationToken ct)
    {
        try
        {
            // Serializable maps to BEGIN IMMEDIATE on SQLite. The write lock is taken up front,
            // so concurrent attempts wait in line and each one sees the previous commit.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (await HasOverlapAsync(booking.RoomId, booking.StartUtc, booking.EndUtc, ct))
                return false;   // leaving the method disposes the transaction, which rolls it back

            await _db.Bookings.AddAsync(booking, ct);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return true;
        }
        catch (Exception ex) when (IsDatabaseBusy(ex))
        {
            // Another request held the lock for too long. Report it as a conflict
            // so the client can simply try again.
            throw new ConflictException(
                "The booking could not be completed because the database is busy. Please try again.");
        }
    }

    public async Task<(List<Booking> Items, int TotalCount)> SearchAsync(
        int? roomId, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Bookings
            .AsNoTracking()   // read-only query, no change tracking needed
            .Where(b => b.Status == BookingStatus.Active);

        if (roomId.HasValue)
            query = query.Where(b => b.RoomId == roomId.Value);

        // A booking is in range when it overlaps [from, to), the same rule as the overlap check.
        if (fromUtc.HasValue)
            query = query.Where(b => b.EndUtc > fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(b => b.StartUtc < toUtc.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(b => b.Room)   // the response needs the room name
            .OrderBy(b => b.StartUtc)
            .ThenBy(b => b.Id)      // tie-breaker keeps page boundaries stable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    // Same rule as Booking.Overlaps, written as a query so the database evaluates it.
    // Back-to-back bookings (end == start) are not overlapping because the comparisons are strict.
    private Task<bool> HasOverlapAsync(
        int roomId, DateTime startUtc, DateTime endUtc, CancellationToken ct) =>
        _db.Bookings.AnyAsync(
            b => b.RoomId == roomId
                 && b.Status == BookingStatus.Active
                 && b.StartUtc < endUtc
                 && b.EndUtc > startUtc,
            ct);

    // EF Core wraps errors raised by SaveChanges in a DbUpdateException,
    // while errors from BEGIN or a query surface as the SqliteException itself.
    private static bool IsDatabaseBusy(Exception ex)
    {
        var sqliteError = ex as SqliteException ?? ex.InnerException as SqliteException;

        return sqliteError is { SqliteErrorCode: SqliteBusy or SqliteLocked };
    }
}