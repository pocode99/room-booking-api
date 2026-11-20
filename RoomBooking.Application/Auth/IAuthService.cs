namespace RoomBooking.Application.Auth;

public interface IAuthService
{
    // Throws ConflictException when the email is already registered.
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);

    // Throws InvalidCredentialsException when the email or password is wrong.
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}