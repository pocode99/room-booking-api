using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Auth;

public interface IUserRepository
{
    // Expects an already normalized (trimmed, lower-case) email.
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    Task AddAsync(User user, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}