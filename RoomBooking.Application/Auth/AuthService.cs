using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens,
        TimeProvider clock,
        ILogger<AuthService> logger)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _logger = logger;
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

        _logger.LogInformation("User {UserId} registered with role {Role}", user.Id, user.Role);

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, ct);

        // The client always receives the same message, whichever check fails,
        // so the API does not reveal which emails exist. Only the server log tells them apart.
        // The attempted email is deliberately not logged: it is raw user input, it may contain
        // control characters, and people sometimes type their password into the email field.
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt: unknown email");
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user {UserId}: wrong password", user.Id);
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponse(_tokens.CreateToken(user), ToResponse(user));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role.ToString());
}