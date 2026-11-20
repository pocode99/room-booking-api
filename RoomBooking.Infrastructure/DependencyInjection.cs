using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Auth;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Rooms;
using RoomBooking.Infrastructure.Authentication;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Infrastructure.Repositories;

namespace RoomBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}