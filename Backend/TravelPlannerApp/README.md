# TravelPlannerApp Backend

ASP.NET Core Minimal API backend for the Travel Itinerary App.

## Stack
- ASP.NET Core 9 Minimal API
- EF Core 9
- MySQL via Pomelo
- SignalR
- Onion Architecture (`Domain`, `Application`, `Infrastructure`, `Api`)

## Solution Layout
- `src/TravelPlannerApp.Domain`: entities and enums
- `src/TravelPlannerApp.Application`: DTOs, validation, use-case services, abstractions
- `src/TravelPlannerApp.Infrastructure`: EF Core persistence, repositories, seed data, migrations
- `src/TravelPlannerApp.Api`: Minimal API endpoints, Swagger, CORS, SignalR, current-user header access

## Prerequisites
- .NET 9 SDK
- MySQL 8.x or SQL Server

## Quick Start
1. Configure environment variables in `.env`.
   - The API loads `.env` from this repository root automatically on startup.
   - Safe placeholders remain in `appsettings*.json`, but real connection strings and passwords should live only in `.env`.
2. Choose your database provider in appsettings.
   - For MySQL: set `"Database": { "Provider": "MySql" }`
   - For SQL Server: set `"Database": { "Provider": "SqlServer" }`
3. Start your database server.
   Option A for MySQL: use Docker Compose from this folder.
   ```bash
   docker compose up -d
   ```
4. Review the active provider in [appsettings.Development.json](C:/Users/User/Documents/Projects/travel-planner-app/Backend/TravelPlannerApp/src/TravelPlannerApp.Api/appsettings.Development.json).
5. If you are using MySQL, apply the initial migration.
   ```bash
   dotnet ef database update --project src/TravelPlannerApp.Infrastructure/TravelPlannerApp.Infrastructure.csproj --startup-project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj
   ```
   If you are using SQL Server, startup will create the schema automatically on first run.
6. Run the API.
   ```bash
   dotnet run --project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj
   ```
7. Open Swagger.
   - `https://localhost:7291/swagger`
   - `http://localhost:5070/swagger`

## Current User Header
Protected itinerary/member/event endpoints use `X-User-Id` instead of full auth.

Seeded users include:
- `user-ava`
- `user-luca`
- `user-mina`
- `user-ethan`
- `user-sofia`
- `user-noah`

Example:
```bash
curl -H "X-User-Id: user-ava" http://localhost:5070/api/itineraries
```

## Migrations
Create a new migration:
```bash
dotnet ef migrations add <MigrationName> --project src/TravelPlannerApp.Infrastructure/TravelPlannerApp.Infrastructure.csproj --startup-project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj --output-dir Persistence/Migrations
```

Apply migrations:
```bash
dotnet ef database update --project src/TravelPlannerApp.Infrastructure/TravelPlannerApp.Infrastructure.csproj --startup-project src/TravelPlannerApp.Api/TravelPlannerApp.Api.csproj
```

## SignalR
Hub route:
- `/hubs/itinerary`

Client methods:
- Call `JoinItinerary(itineraryId)` to subscribe to an itinerary group
- Call `LeaveItinerary(itineraryId)` to unsubscribe
- Listen for `itineraryUpdated`

## Seed Data
The app seeds:
- 6 users
- 5 itineraries
- itinerary memberships
- events aligned with the frontend mock data
- event audit history entries

Seed data is inserted automatically on startup after migrations run.
