using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Auth;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Rooms;

namespace RoomBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}