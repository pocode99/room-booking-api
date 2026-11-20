namespace RoomBooking.Application.Auth;

public interface IPasswordHasher
{
    // Returns a self-describing hash string that includes the salt and parameters.
    string Hash(string password);

    // Returns true when the password matches the stored hash.
    bool Verify(string password, string hash);
}