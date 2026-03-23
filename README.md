# NoorLocator

NoorLocator is a location-based web platform for discovering Shia Islamic centers, publishing majalis through verified center managers, and protecting data integrity through moderated workflows.

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.

## Phase 2 Summary

Phase 2 builds on the existing scaffold and adds:

- persisted EF Core models and MySQL schema
- JWT authentication and role-based authorization
- secure password hashing
- `/api/auth/register`, `/api/auth/login`, and `/api/auth/me`
- seeded demo accounts, languages, centers, and manager assignment
- audit logging for authentication-critical events
- frontend token storage, authenticated API calls, and role-aware navbar behavior
- initial EF Core migration files

## Architecture

```text
NoorLocator.sln
|-- NoorLocator.Api
|-- NoorLocator.Application
|-- NoorLocator.Domain
|-- NoorLocator.Infrastructure
|-- frontend
```

- `NoorLocator.Api`: HTTP endpoints, JWT middleware, Swagger, static frontend hosting, and startup migration/seed execution.
- `NoorLocator.Application`: DTOs, service interfaces, validation contracts, shared response models, and auth configuration contracts.
- `NoorLocator.Domain`: core entities and enums from the specification, plus Phase 2 traceability/auth entities.
- `NoorLocator.Infrastructure`: EF Core `DbContext`, Pomelo MySQL provider configuration, entity configurations, migrations, seeding, password hashing, JWT token generation, auditing, and concrete services.
- `frontend`: static HTML/CSS/JS pages with login/register flows, local token storage, and navbar auth state handling.

## Implemented Models

Required models:

- `User`
- `Center`
- `CenterRequest`
- `CenterManager`
- `Majlis`
- `Language`
- `MajlisLanguage`
- `CenterLanguage`
- `CenterLanguageSuggestion`
- `Suggestion`

Additional Phase 2 models:

- `ManagerRequest`
- `AuditLog`
- `RefreshToken`

## Seeded Data

The app seeds these accounts on startup through [NoorLocatorDbInitializer.cs](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Infrastructure/Seeding/NoorLocatorDbInitializer.cs):

- Admin
  - Email: `admin@noorlocator.local`
  - Password: `Admin123!Pass`
- Manager
  - Email: `manager@noorlocator.local`
  - Password: `Manager123!Pass`
- User
  - Email: `user@noorlocator.local`
  - Password: `User123!Pass`

Seeded reference/content data:

- Languages: Arabic, Swedish, English, Farsi, Urdu
- 3 demo centers in Copenhagen, Stockholm, and Helsinki
- 1 approved manager assignment for the seeded manager account
- 1 sample majlis linked to the Copenhagen demo center

## Auth and Authorization

- Guests are anonymous users and can access read-only public endpoints.
- Registered users are created only through `/api/auth/register` and always receive the `User` role.
- Manager access is approval-based and backed by `ManagerRequests` and `CenterManagers`.
- Admin access is seeded and not available through self-registration.
- Passwords are stored using PBKDF2 with per-password salt.
- JWT access tokens are returned from register/login.
- Refresh token records are persisted for future token lifecycle work.
- Audit logs are written for login and registration success/failure events.

## Setup

1. Open the repo root:

   ```powershell
   cd C:\Users\alhil\Desktop\NoorLocator\NoorLocator
   ```

2. Update the MySQL connection string, username, password, and JWT settings if needed:
   - [appsettings.json](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Api/appsettings.json)
   - [appsettings.Development.json](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Api/appsettings.Development.json)

   The default development database target is:

   ```text
   Server=127.0.0.1;Port=3306;Database=Noorlocator
   ```

3. Restore and build:

   ```powershell
   dotnet restore
   dotnet build NoorLocator.sln
   ```

4. Apply migrations manually if you want to do it ahead of startup:

   ```powershell
   dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
   ```

5. Run the app:

   ```powershell
   dotnet run --project .\NoorLocator.Api\NoorLocator.Api.csproj
   ```

The API also attempts to apply migrations and seed data automatically during startup.

## Default URLs

- App: `https://localhost:7132/`
- HTTP fallback: `http://localhost:5141/`
- Swagger: `https://localhost:7132/swagger`
- Health: `https://localhost:7132/api/health`

## Migrations

Initial migration files are located in:

- [20260323192750_InitialCreate.cs](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Infrastructure/Persistence/Migrations/20260323192750_InitialCreate.cs)
- [NoorLocatorDbContextModelSnapshot.cs](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Infrastructure/Persistence/Migrations/NoorLocatorDbContextModelSnapshot.cs)

## Notes

- The Phase 2 frontend stores auth state in `localStorage`.
- The navbar changes based on the stored authenticated user role.
- Center and majlis read endpoints now return seeded database content instead of placeholder responses.
- The project now targets MySQL through Pomelo's EF Core provider.
- The design-time provider uses a configurable MySQL server version value in `MySql:ServerVersion` so migrations can be generated without database auto-detection.
- Update the MySQL username and password in the development connection string before running migrations or startup seeding.
