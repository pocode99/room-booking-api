namespace RoomBooking.Application.Auth;

public record AuthResponse(string AccessToken, UserResponse User);