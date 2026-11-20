using RoomBooking.Application.Auth;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;

namespace RoomBooking.Infrastructure.Persistence;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Rooms.Any()) return;   // already seeded, do nothing

        db.Rooms.AddRange(
            new Room { Name = "Orion", Capacity = 4 },
            new Room { Name = "Vega", Capacity = 8 },
            new Room { Name = "Atlas", Capacity = 12 },
            new Room { Name = "Nova", Capacity = 20 });

        db.SaveChanges();
    }

    // Creates the admin account once. Credentials come from configuration, never from code.
    public static void SeedAdmin(
        AppDbContext db,
        IPasswordHasher hasher,
        TimeProvider clock,
        string email,
        string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (db.Users.Any(u => u.Email == normalizedEmail)) return;   // already exists

        db.Users.Add(new User
        {
            Email = normalizedEmail,
            PasswordHash = hasher.Hash(password),
            FullName = "Administrator",
            Role = UserRole.Admin,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        });

        db.SaveChanges();
    }
}