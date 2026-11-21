using Microsoft.AspNetCore.Mvc.Testing;

namespace RoomBooking.Tests.Integration;

// Starts the real application in memory, backed by a throwaway SQLite file.
public class ApiFactory : WebApplicationFactory<Program>
{
    // Test-only credentials. They are not real secrets and only exist for the test run.
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Test-Admin-Pass-123!";

    private const string JwtKey = "integration-tests-signing-key-with-more-than-32-bytes";

    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"roombooking-tests-{Guid.NewGuid():N}.db");

    private static readonly string[] Variables =
    {
        "ConnectionStrings__Default", "Jwt__Key", "Seed__AdminEmail", "Seed__AdminPassword"
    };

    public ApiFactory()
    {
        // Program.cs reads these settings while it is still building the host,
        // so they are passed as environment variables, which exist before any configuration is read.
        // Pooling=False releases the file as soon as the app stops, so it can be deleted afterwards.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_dbPath};Pooling=False");
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Seed__AdminEmail", AdminEmail);
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;

        foreach (var name in Variables)
            Environment.SetEnvironmentVariable(name, null);

        // Best-effort cleanup of the temporary database and its WAL side files.
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try { File.Delete(_dbPath + suffix); }
            catch (IOException) { }
        }
    }
}