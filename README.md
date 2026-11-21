# Room Booking API

![CI](https://github.com/pocode99/room-booking-api/actions/workflows/ci.yml/badge.svg)

A REST API for booking meeting rooms inside a company, built with ASP.NET Core.
It is a portfolio project that focuses on layered design, authentication,
handling concurrent requests, and automated testing.

## Who uses it
- **Employees (User)**: browse rooms, book a room for a time slot, cancel their own bookings.
- **Administrators (Admin)**: manage rooms (create, update, delete) and cancel any booking.

## Features
- Register and log in (JWT authentication)
- Manage rooms (name, capacity)
- Book a room for a time range, with conflict detection
- Search bookings by room and time range, with pagination
- Cancel a booking
- Liveness and readiness health checks

## Business rules
1. A booking's start time must be before its end time.
2. A booking cannot start in the past.
3. A booking cannot be longer than 4 hours.
4. Two bookings for the same room cannot overlap. Back-to-back bookings
   (one ends at 10:00, the next starts at 10:00) are allowed.
5. Only the booking owner or an Admin can cancel a booking.
6. All times are stored in UTC.

## Tech stack
- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core with SQLite
- JWT bearer authentication, PBKDF2 password hashing
- xUnit, Moq and `WebApplicationFactory` for unit and integration tests
- GitHub Actions (build, test, container smoke test) and Docker

## Architecture
The solution is split into layers. Dependencies point inward: `Domain` knows
nothing about the other projects.

| Project | Responsibility |
|---|---|
| `RoomBooking.Domain` | Entities, enums and rules that belong to an entity (for example `Booking.Overlaps`) |
| `RoomBooking.Application` | Use cases (services), DTOs with validation, and the interfaces it needs (repositories, token service, password hasher) |
| `RoomBooking.Infrastructure` | EF Core, migrations, repository implementations, JWT creation, password hashing |
| `RoomBooking.Api` | Controllers, authentication setup, global error handling, health checks |
| `RoomBooking.Tests` | Unit tests (mocks) and integration tests (the real app with a temporary SQLite database) |

```
Api  ->  Application  ->  Domain
Api  ->  Infrastructure  ->  Application
```

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### 1. Clone
```bash
git clone https://github.com/<your-username>/room-booking-api.git
cd room-booking-api
```

### 2. Configure
No secret is stored in the repository. The application refuses to start
without a signing key for the tokens.

PowerShell:
```powershell
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$rng.Dispose()
$env:Jwt__Key = [Convert]::ToBase64String($bytes)

# Optional: create an admin account on first start
$env:Seed__AdminEmail = "admin@example.com"
$env:Seed__AdminPassword = "<choose-a-password>"
```

Bash:
```bash
export Jwt__Key="$(openssl rand -base64 48)"

# Optional: create an admin account on first start
export Seed__AdminEmail="admin@example.com"
export Seed__AdminPassword="<choose-a-password>"
```

| Setting | Required | Description |
|---|---|---|
| `Jwt__Key` | Yes | Secret used to sign tokens, at least 32 bytes |
| `Seed__AdminEmail`, `Seed__AdminPassword` | No | Creates an admin account on first start. Without them no admin exists |
| `ConnectionStrings__Default` | No | SQLite connection string. Default: `Data Source=roombooking.db` |

During development you can use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
instead of environment variables:
```bash
dotnet user-secrets set "Jwt:Key" "<at-least-32-bytes>" --project RoomBooking.Api
```

### 3. Run
```bash
dotnet run --project RoomBooking.Api
```
The API listens on `http://localhost:5016`. The database file is created, migrated
and filled with four sample rooms on the first start.

### 4. Try it
- Open **http://localhost:5016/swagger** (Swagger UI, development only).
- Or use the ready-made requests in `RoomBooking.Api/Requests/*.http` with the
  VS Code *REST Client* extension. `rooms.http` and `bookings.http` read the admin
  password from a git-ignored file `RoomBooking.Api/Requests/.env`:
  ```
  ADMIN_PASSWORD=<the password you set in Seed__AdminPassword>
  ```
- Health checks: `GET /health/live` and `GET /health/ready`.

The accounts and passwords used in the `.http` files and tests (for example
`alice@example.com`) are demo data only.

### Run the tests
```bash
dotnet test
```

### Run with Docker
```bash
docker build -t room-booking-api .
docker run --rm -p 8080:8080 \
  -e Jwt__Key="<at-least-32-bytes>" \
  -v roombooking-data:/data \
  room-booking-api
```
The database is stored in the `/data` volume, so it survives container restarts.
The image runs as a non-root user.