using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.Title).IsRequired().HasMaxLength(200);
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(b => new { b.RoomId, b.StartUtc });
        });
    }
}