using RoomBooking.Domain.Enums;

namespace RoomBooking.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAtUtc { get; set; }

    public List<Booking> Bookings { get; set; } = new();
}