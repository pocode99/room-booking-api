using RoomBooking.Domain.Entities;

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
}