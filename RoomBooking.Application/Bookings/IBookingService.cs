using RoomBooking.Application.Common;

namespace RoomBooking.Application.Bookings;

public interface IBookingService
{
    // Throws NotFoundException (room), BusinessRuleException (invalid times),
    // or ConflictException (overlapping booking).
    Task<BookingResponse> CreateAsync(int userId, CreateBookingRequest request, CancellationToken ct);

    // Throws NotFoundException when the booking does not exist.
    Task<BookingResponse> GetByIdAsync(int id, CancellationToken ct);

    // Throws BusinessRuleException when "from" is not earlier than "to".
    Task<PagedResult<BookingResponse>> SearchAsync(BookingSearchRequest request, CancellationToken ct);

    // Throws NotFoundException, or ForbiddenException when the caller is neither
    // the booking owner nor an admin.
    Task CancelAsync(int id, int requesterId, bool isAdmin, CancellationToken ct);
}