using RoomBooking.Application.Exceptions;
using RoomBooking.Application.Rooms;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Application.Common;

namespace RoomBooking.Application.Bookings;

public class BookingService : IBookingService
{
    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(4);

    private readonly IBookingRepository _bookings;
    private readonly IRoomRepository _rooms;
    private readonly TimeProvider _clock;

    public BookingService(
        IBookingRepository bookings,
        IRoomRepository rooms,
        TimeProvider clock)
    {
        _bookings = bookings;
        _rooms = rooms;
        _clock = clock;
    }

    public async Task<BookingResponse> CreateAsync(
        int userId, CreateBookingRequest request, CancellationToken ct)
    {
        // Normalize to UTC first, so every rule below compares like with like.
        var startUtc = request.Start.UtcDateTime;
        var endUtc = request.End.UtcDateTime;
        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        if (startUtc >= endUtc)
            throw new BusinessRuleException("The start time must be before the end time.");

        if (startUtc < nowUtc)
            throw new BusinessRuleException("A booking cannot start in the past.");

        if (endUtc - startUtc > MaxDuration)
            throw new BusinessRuleException("A booking cannot be longer than 4 hours.");

        var room = await _rooms.GetByIdAsync(request.RoomId, ct)
            ?? throw new NotFoundException($"Room {request.RoomId} was not found.");

        var booking = new Booking
        {
            RoomId = room.Id,
            Room = room,   // lets the response include the room name without another query
            UserId = userId,
            Title = request.Title.Trim(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = BookingStatus.Active,
            CreatedAtUtc = nowUtc
        };

        // The overlap check and the insert happen atomically inside the repository,
        // so two concurrent requests cannot both book the same slot.
        if (!await _bookings.TryAddIfAvailableAsync(booking, ct))
            throw new ConflictException("The room is already booked for part of this time range.");

        return ToResponse(booking);
    }

    public async Task<BookingResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var booking = await GetBookingOrThrowAsync(id, ct);
        return ToResponse(booking);
    }

    public async Task<PagedResult<BookingResponse>> SearchAsync(
    BookingSearchRequest request, CancellationToken ct)
    {
        var fromUtc = request.From?.UtcDateTime;
        var toUtc = request.To?.UtcDateTime;

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value >= toUtc.Value)
            throw new BusinessRuleException("The 'from' time must be earlier than the 'to' time.");

        var (items, totalCount) = await _bookings.SearchAsync(
            request.RoomId, fromUtc, toUtc, request.Page, request.PageSize, ct);

        return new PagedResult<BookingResponse>(
            items.Select(ToResponse).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task CancelAsync(int id, int requesterId, bool isAdmin, CancellationToken ct)
    {
        var booking = await GetBookingOrThrowAsync(id, ct);

        if (!isAdmin && booking.UserId != requesterId)
            throw new ForbiddenException("You can only cancel your own bookings.");

        // Cancelling twice is harmless, so treat it as already done.
        if (booking.Status == BookingStatus.Cancelled) return;

        booking.Cancel();   // the rule "cancel = change Status" lives in the domain entity
        await _bookings.SaveChangesAsync(ct);
    }

    private async Task<Booking> GetBookingOrThrowAsync(int id, CancellationToken ct)
    {
        return await _bookings.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Booking {id} was not found.");
    }

    private static BookingResponse ToResponse(Booking booking) =>
        new(
            booking.Id,
            booking.RoomId,
            booking.Room.Name,
            booking.UserId,
            booking.Title,
            AsUtc(booking.StartUtc),
            AsUtc(booking.EndUtc),
            booking.Status.ToString(),
            AsUtc(booking.CreatedAtUtc));

    // SQLite returns DateTime values with Kind = Unspecified. Marking them as UTC makes
    // the API serialize them with an explicit +00:00 offset instead of an ambiguous local time.
    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}