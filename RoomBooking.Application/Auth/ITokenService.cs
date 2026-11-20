using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Auth;

public interface ITokenService
{
    // Creates a signed JWT access token for the given user.
    string CreateToken(User user);
}