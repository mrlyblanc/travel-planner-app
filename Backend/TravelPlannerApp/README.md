# TravelPlannerApp Backend

ASP.NET Core Minimal API backend for the Travel Itinerary App.

## Stack
- ASP.NET Core 9 Minimal API
- EF Core 9
- SQL Server
- SignalR
- JWT bearer authentication
- Serilog
- Onion Architecture (`Domain`, `Application`, `Infrastructure`, `Api`)

## Solution Layout
- `src/TravelPlannerApp.Domain`: entities and enums
- `src/TravelPlannerApp.Application`: DTOs, validation, use-case services, abstractions
- `src/TravelPlannerApp.Infrastructure`: EF Core persistence, repositories, seed data, migrations
- `src/TravelPlannerApp.Api`: Minimal API endpoints, Swagger, CORS, SignalR, JWT auth, authorization policies
- `tests/TravelPlannerApp.Application.Tests`: unit tests for business rules and edge cases
- `tests/TravelPlannerApp.Api.IntegrationTests`: integration tests for Minimal API endpoints

## Prerequisites
- .NET 9 SDK
- SQL Server
  - local SQL Server / SQL Express, or
  - the included Docker Compose SQL Server container

## Configuration
1. Create `.env` in the repository root from [.env.example](C:\Users\User\Documents\Projects\travel-planner-app\Backend\TravelPlannerApp\.env.example).
2. Set a valid SQL Server connection string in `ConnectionStrings__SqlServer`.
3. Set a strong JWT secret in `Jwt__Secret`.
4. Set the seed password in `Seed__DefaultUserPassword`.

Tracked `appsettings*.json` files only contain safe placeholders or local defaults. Real secrets should stay in `.env`.

## Quick Start
1. If you want a local SQL Server container, start it from this folder:
   ```bash
   docker compose up -d
   ```
2. Apply migrations:
   ```bash
   dotnet ef database update --project src/TravelPlannerApp.Infrastructure/TravelPlannerApp.Infrastructure.csproj --startup-project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj
   ```
3. Run the API:
   ```bash
   dotnet run --project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj
   ```
4. Open Swagger:
   - `https://localhost:7291/swagger`
   - `http://localhost:5070/swagger`

The API also applies migrations on startup for a fresh SQL Server database. Older dev databases without migration history are patched for compatibility and baselined on startup.

## Required .env Keys
- `ConnectionStrings__SqlServer`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Secret`
- `Jwt__TokenLifetimeMinutes`
- `Seed__DefaultUserPassword`

Optional when using Docker Compose:
- `MSSQL_SA_PASSWORD`

## Authentication
The API uses JWT bearer authentication.

Login example:
```bash
curl -X POST http://localhost:5070/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"ava.santos@globejet.com\",\"password\":\"<Seed__DefaultUserPassword>\"}"
```

Authenticated request example:
```bash
curl http://localhost:5070/api/itineraries ^
  -H "Authorization: Bearer <jwt>"
```

Current-user endpoint:
- `GET /api/auth/me`

## Authorization
The API uses resource ownership for write access.

Ownership rules:
- user profile updates: only the profile owner
- itinerary updates: only the itinerary creator
- itinerary member replacement: only the itinerary creator
- event updates and deletes: any itinerary member

General access rules:
- authenticated users can list and read users
- itinerary and event reads require itinerary membership
- itinerary members can create, update, and delete events

## API Versioning
- Header-based versioning
- Send `X-Api-Version: 1.0`
- URIs remain clean: `/api/users`, `/api/itineraries`, `/api/events`

## Concurrency
- Optimistic concurrency is enabled on mutable resources
- Single-resource reads return `ETag`
- Mutating endpoints require `If-Match`
- Stale updates return `412 Precondition Failed`
- Missing preconditions return `428 Precondition Required`

## Seed Data
The app seeds:
- 6 users
- 5 itineraries
- itinerary memberships
- events aligned with the frontend mock data
- event audit history entries

Seeded users include:
- `user-ava`
- `user-luca`
- `user-mina`
- `user-ethan`
- `user-sofia`
- `user-noah`

All seeded users use the password from `Seed__DefaultUserPassword`.

## Migrations
Create a new migration:
```bash
dotnet ef migrations add <MigrationName> --project src/TravelPlannerApp.Infrastructure/TravelPlannerApp.Infrastructure.csproj --startup-project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj --output-dir Persistence/Migrations
```

Apply migrations:
```bash
dotnet ef database update --project src/TravelPlannerApp.Infrastructure/TravelPlannerApp.Infrastructure.csproj --startup-project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj
```

If you already have a legacy SQL Server database created before migrations were in place, start the API once before using `dotnet ef database update` again. Startup will create the migration history baseline for that database.

## Logging
- Serilog console logging
- Rolling file logs under `logs/`
- Request logging enabled for API traffic

## SignalR
Hub route:
- `/hubs/itinerary`

Client methods:
- Call `JoinItinerary(itineraryId)` to subscribe to an itinerary group
- Call `LeaveItinerary(itineraryId)` to unsubscribe

Authenticate the hub connection with the same JWT using the SignalR `access_token` query-string pattern.
