using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RoomBooking.Application.Auth;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Infrastructure.Authentication;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    public TokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;

        // HMAC-SHA256 needs a key of at least 256 bits (32 bytes).
        if (string.IsNullOrWhiteSpace(_options.Key) || Encoding.UTF8.GetByteCount(_options.Key) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
    }

    public string CreateToken(User user)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.FullName),
                new Claim("role", user.Role.ToString())
            }),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_options.ExpiresMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}