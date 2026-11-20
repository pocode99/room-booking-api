namespace RoomBooking.Infrastructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    // Secret used to sign tokens. Must come from user secrets or environment variables.
    public string Key { get; set; } = "";

    public int ExpiresMinutes { get; set; } = 60;
}