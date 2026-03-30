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

Configuration precedence is now Azure-friendly:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Azure Key Vault, when `Azure__KeyVault__VaultUri` is configured
4. environment variables, including `.env` and Azure App Service settings
5. command-line arguments

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
- `Database__ApplyMigrationsOnStartup`
- `Seed__Enabled`
- `Seed__DefaultUserPassword`

Optional when using Docker Compose:
- `MSSQL_SA_PASSWORD`

Optional for Azure:
- `Azure__KeyVault__VaultUri`
- `Azure__ManagedIdentityClientId`

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
- event updates and deletes: only the event creator

General access rules:
- authenticated users can search users
- itinerary and event reads require itinerary membership
- itinerary members can create events
- itinerary members can read shared itinerary events
- only the event creator can update or delete that event

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

Seed data is environment-controlled:
- development default: enabled
- production default: disabled

Startup migrations are also environment-controlled:
- development default: enabled
- production default: disabled

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

## Azure Configuration
The API is now prepared for Azure App Service and Azure Key Vault.

### Supported Azure secret/config sources
- Azure App Service application settings such as `Jwt__Secret`
- Azure App Service connection strings such as `SQLAZURECONNSTR_SqlServer`
- Azure Key Vault secrets via `Azure__KeyVault__VaultUri`

### Key Vault secret naming
Use `--` in secret names where configuration uses `:`.

Examples:
- `ConnectionStrings--SqlServer`
- `Jwt--Secret`
- `Jwt--Issuer`
- `Jwt--Audience`
- `Seed--DefaultUserPassword`

### Managed identity
For Azure deployment, use managed identity instead of client secrets.

- system-assigned identity:
  - set `Azure__KeyVault__VaultUri`
  - grant the app access to Key Vault secrets
- user-assigned identity:
  - also set `Azure__ManagedIdentityClientId`

### Recommended production settings
- `ASPNETCORE_ENVIRONMENT=Production`
- `Database__ApplyMigrationsOnStartup=false`
- `Seed__Enabled=false`

### Recommended Azure App Service settings
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__TokenLifetimeMinutes`
- `Jwt__RefreshTokenLifetimeDays`
- `Cors__AllowedOrigins__0`
- `Azure__KeyVault__VaultUri`

Keep actual secrets in Key Vault or App Service settings, not in tracked files.

## GitHub Actions CI/CD
A GitHub Actions workflow is included at [travel-planner-api.yml](C:\Users\User\Documents\Projects\travel-planner-app\.github\workflows\travel-planner-api.yml).

What it does:
- build the backend solution
- run unit and integration tests
- publish the API artifact
- deploy the published artifact to Azure App Service

Trigger behavior:
- push to `main` for backend changes
- pull requests to `main` for backend changes
- manual `workflow_dispatch`

Authentication model:
- OpenID Connect to Azure App Service
- no publish profile required
- recommended by Azure over long-lived deployment secrets

Required GitHub repository secrets:
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Required GitHub repository variables:
- `AZURE_WEBAPP_NAME`

Optional GitHub repository variables:
- `AZURE_WEBAPP_SLOT`

### OIDC setup
1. Create or choose an Azure service principal for GitHub Actions deployment.
2. Add a federated credential for your GitHub repository and branch.
3. Grant that identity access to the App Service.

Minimum practical role:
- `Website Contributor` scoped to the target App Service

### Deployment notes
- The workflow deploys only the app artifact.
- It does not run EF Core migrations against production.
- Keep production app settings as:
  - `Database__ApplyMigrationsOnStartup=false`
  - `Seed__Enabled=false`

If you want database migrations in CI/CD, add a separate controlled step with explicit approval or a separate migration workflow.

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
