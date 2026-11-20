using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.Rooms;

public record UpdateRoomRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = "";

    [Range(1, 1000)]
    public int Capacity { get; init; }
}