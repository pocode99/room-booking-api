namespace RoomBooking.Application.Rooms;

public interface IRoomService
{
    Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken ct);

    // Throws NotFoundException when the room does not exist.
    Task<RoomResponse> GetByIdAsync(int id, CancellationToken ct);

    // Throws ConflictException when the name is already taken.
    Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken ct);

    // Throws NotFoundException or ConflictException.
    Task<RoomResponse> UpdateAsync(int id, UpdateRoomRequest request, CancellationToken ct);

    // Throws NotFoundException, or ConflictException when the room has upcoming bookings.
    Task DeleteAsync(int id, CancellationToken ct);
}