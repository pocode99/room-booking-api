using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Rooms;

namespace RoomBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IRoomService, RoomService>();

        return services;
    }
}