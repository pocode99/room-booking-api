using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.Auth;

public record LoginRequest
{
    [Required]
    [StringLength(256)]
    public string Email { get; init; } = "";

    [Required]
    [StringLength(100)]
    public string Password { get; init; } = "";
}