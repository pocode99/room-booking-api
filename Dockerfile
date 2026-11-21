# ---------- Build stage: compiles and publishes the API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first, so the restore layer stays cached
# until a package reference actually changes.
COPY RoomBooking.Domain/RoomBooking.Domain.csproj RoomBooking.Domain/
COPY RoomBooking.Application/RoomBooking.Application.csproj RoomBooking.Application/
COPY RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj RoomBooking.Infrastructure/
COPY RoomBooking.Api/RoomBooking.Api.csproj RoomBooking.Api/
RUN dotnet restore RoomBooking.Api/RoomBooking.Api.csproj

# Now copy the rest of the source and publish.
COPY . .
RUN dotnet publish RoomBooking.Api/RoomBooking.Api.csproj \
    --configuration Release --no-restore --output /app/publish

# ---------- Runtime stage: only the ASP.NET runtime and the published output ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The SQLite database lives in /data, which is meant to be mounted as a volume.
# The directory must be writable by the non-root user that runs the app.
RUN mkdir /data && chown $APP_UID /data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Default="Data Source=/data/roombooking.db"

# Run as a non-root user.
USER $APP_UID

EXPOSE 8080
VOLUME /data
ENTRYPOINT ["dotnet", "RoomBooking.Api.dll"]