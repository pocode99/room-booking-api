using RoomBooking.Application.Exceptions;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly TimeProvider _clock;

    public AuthService(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens,
        TimeProvider clock)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        if (await _users.EmailExistsAsync(email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            // Public registration always creates a regular user. Admins are seeded.
            Role = UserRole.User,
            CreatedAtUtc = _clock.GetUtcNow().UtcDateTime
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, ct);

        // Use one message for both cases, so the API does not reveal which emails exist.
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException("Invalid email or password.");

        return new AuthResponse(_tokens.CreateToken(user), ToResponse(user));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role.ToString());
}