using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Rooms;

namespace RoomBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _rooms;

    public RoomsController(IRoomService rooms)
    {
        _rooms = rooms;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetAll(CancellationToken ct)
        => Ok(await _rooms.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _rooms.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(CreateRoomRequest request, CancellationToken ct)
    {
        var room = await _rooms.CreateAsync(request, ct);

        // Returns 201 Created with a Location header pointing to GET api/rooms/{id}.
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomResponse>> Update(int id, UpdateRoomRequest request, CancellationToken ct)
        => Ok(await _rooms.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _rooms.DeleteAsync(id, ct);
        return NoContent();
    }
}