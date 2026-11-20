using RoomBooking.Application.Exceptions;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Rooms;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;
    private readonly TimeProvider _clock;

    public RoomService(IRoomRepository rooms, TimeProvider clock)
    {
        _rooms = rooms;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken ct)
    {
        var rooms = await _rooms.GetAllAsync(ct);
        return rooms.Select(ToResponse).ToList();
    }

    public async Task<RoomResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var room = await GetRoomOrThrowAsync(id, ct);
        return ToResponse(room);
    }

    public async Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await _rooms.NameExistsAsync(name, excludeId: null, ct))
            throw new ConflictException($"A room named '{name}' already exists.");

        var room = new Room { Name = name, Capacity = request.Capacity };

        await _rooms.AddAsync(room, ct);
        await _rooms.SaveChangesAsync(ct);

        return ToResponse(room);
    }

    public async Task<RoomResponse> UpdateAsync(int id, UpdateRoomRequest request, CancellationToken ct)
    {
        var room = await GetRoomOrThrowAsync(id, ct);
        var name = request.Name.Trim();

        // Exclude the room being edited, so keeping its own name is allowed.
        if (await _rooms.NameExistsAsync(name, excludeId: id, ct))
            throw new ConflictException($"A room named '{name}' already exists.");

        room.Name = name;
        room.Capacity = request.Capacity;

        await _rooms.SaveChangesAsync(ct);

        return ToResponse(room);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var room = await GetRoomOrThrowAsync(id, ct);

        var utcNow = _clock.GetUtcNow().UtcDateTime;
        if (await _rooms.HasUpcomingBookingsAsync(id, utcNow, ct))
            throw new ConflictException("The room has upcoming bookings and cannot be deleted.");

        _rooms.Remove(room);
        await _rooms.SaveChangesAsync(ct);
    }

    private async Task<Room> GetRoomOrThrowAsync(int id, CancellationToken ct)
    {
        return await _rooms.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Room {id} was not found.");
    }

    private static RoomResponse ToResponse(Room room) =>
        new(room.Id, room.Name, room.Capacity);
}