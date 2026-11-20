namespace RoomBooking.Application.Auth;

public record UserResponse(int Id, string Email, string FullName, string Role);