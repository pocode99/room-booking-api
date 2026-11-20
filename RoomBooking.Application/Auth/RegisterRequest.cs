using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.Auth;

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = "";

    // The upper bound prevents very long inputs from making hashing expensive.
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = "";

    [Required]
    [StringLength(100)]
    public string FullName { get; init; } = "";
}