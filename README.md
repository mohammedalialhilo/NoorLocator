# NoorLocator

NoorLocator is a moderated web platform for discovering Shia Islamic centers, browsing upcoming majalis, and contributing community updates through role-based workflows.

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.

## What Is Included

- Public center discovery with nearest-center lookup, search, filtering, and center detail pages
- JWT authentication with `Guest`, `User`, `Manager`, and `Admin` roles
- Moderation-first contribution workflows for center requests, language suggestions, manager requests, and product feedback
- Manager-only majlis CRUD with center assignment enforcement
- Admin moderation dashboard, center management, user visibility, and audit logs
- Richer seeded demo data for centers, languages, and majalis
- Swagger/OpenAPI documentation
- Docker support for the API and MySQL
- Unit and integration tests
- PWA basics with a web manifest and service worker shell caching

## Stack

- Frontend: HTML, CSS, JavaScript
- Backend: ASP.NET Core Web API
- Database: MySQL with EF Core and Pomelo
- Auth: JWT
- Tests: xUnit

## Architecture

```text
NoorLocator.sln
|-- NoorLocator.Api
|-- NoorLocator.Application
|-- NoorLocator.Domain
|-- NoorLocator.Infrastructure
|-- frontend
`-- tests
    |-- NoorLocator.UnitTests
    `-- NoorLocator.IntegrationTests
```

- `NoorLocator.Api`
  Hosts controllers, auth setup, exception handling, Swagger, static frontend hosting, and startup initialization.
- `NoorLocator.Application`
  Contains DTOs, service contracts, validation, and shared response models.
- `NoorLocator.Domain`
  Holds the core entities and enums.
- `NoorLocator.Infrastructure`
  Implements EF Core, MySQL setup, migrations, seeding, JWT generation, password hashing, auditing, and concrete services.
- `frontend`
  Contains the branded static client that consumes only the live API.
- `tests`
  Covers service-level logic and key HTTP flows.

## Roles

- `Guest`
  Unauthenticated public visitor who can browse published centers and majalis.
- `User`
  Authenticated community member who can submit moderated contributions.
- `Manager`
  Authenticated user assigned to one or more approved centers and allowed to manage majalis there.
- `Admin`
  Full moderation and management access across the platform.

## Seeded Accounts

- Admin: `admin@noorlocator.local` / `Admin123!Pass`
- Manager: `manager@noorlocator.local` / `Manager123!Pass`
- User: `user@noorlocator.local` / `User123!Pass`

## Seeded Demo Data

- Languages: Arabic, Swedish, English, Farsi, Urdu
- Demo centers in Copenhagen, Stockholm, Helsinki, Oslo, and Aarhus
- Approved manager assignments for the seeded manager account
- Multiple public demo majalis across the seeded centers

## Local Setup

### Prerequisites

- .NET SDK 10
- MySQL 8.x

### Development Configuration

Development settings live in:

- `NoorLocator.Api/appsettings.Development.json`

Safer shared defaults and production placeholders live in:

- `NoorLocator.Api/appsettings.json`
- `NoorLocator.Api/appsettings.Production.json`

For production-style deployments, prefer environment variables or user-secrets over committed credentials.

### Run Locally

```powershell
cd C:\Users\alhil\Desktop\NoorLocator\NoorLocator
dotnet restore
dotnet build NoorLocator.sln
dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
dotnet run --project .\NoorLocator.Api\NoorLocator.Api.csproj
```

### Default URLs

- App: `https://localhost:7132/`
- HTTP fallback: `http://localhost:5141/`
- Swagger: `https://localhost:7132/swagger`
- Health: `https://localhost:7132/api/health`

## Migrations

Migration files live in:

- `NoorLocator.Infrastructure/Persistence/Migrations`

Useful commands:

```powershell
dotnet ef migrations add YourMigrationName --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
```

## Docker

Build and run the app with MySQL:

```powershell
docker compose up --build
```

Docker services:

- API: `http://localhost:8080`
- MySQL: `localhost:3306`

Files:

- `Dockerfile`
- `docker-compose.yml`
- `.dockerignore`

Important:

- Replace `Jwt__Key` in `docker-compose.yml` before using it beyond local development.
- The container uses `Frontend__RelativeRootPath=frontend`, and the frontend assets are copied into the publish output automatically.

## Swagger And API Coverage

Swagger now includes:

- XML-comment summaries for the API surface
- JWT bearer security definition
- Default error response documentation
- Documented public, authenticated, manager, and admin endpoints

Main endpoint groups:

- Auth: `/api/auth/*`
- Public discovery: `/api/centers/*`, `/api/languages`, `/api/majalis`
- User contributions: `/api/center-requests`, `/api/suggestions`, `/api/center-language-suggestions`, `/api/manager/request`
- Manager: `/api/manager/*`, `/api/majalis/*`
- Admin: `/api/admin/*`

## Testing

Run all tests:

```powershell
dotnet test NoorLocator.sln
```

Included coverage:

- Unit tests for center discovery and center request workflows
- Integration tests for registration, protected routes, public center search, and admin authorization

Test projects:

- `tests/NoorLocator.UnitTests`
- `tests/NoorLocator.IntegrationTests`

## Frontend Notes

- The frontend consumes only the live API
- Auth state is stored in `localStorage`
- The navbar updates by role and now includes a mobile toggle
- Toasts, loading states, empty states, and responsive layouts were polished in Phase 7
- PWA basics are included through `site.webmanifest` and `service-worker.js`

## Production-Readiness Notes

- Unhandled exceptions are now returned through a consistent API response shape
- Validation failures include trace-friendly metadata
- Admin access is enforced server-side through policy-based authorization
- DTO-based write endpoints reduce overposting risk
- JWT configuration is validated more strictly outside development
- Open CORS is allowed only in development and testing when origins are not explicitly configured
- Published API output now includes the static frontend assets for container and publish scenarios

## Future Roadmap

- Refresh token rotation and logout/revocation flows
- Multi-language UI scaffolding for English, Arabic, and Swedish
- Favorites or saved centers
- Calendar-style public majalis browsing
- Nearby majalis and notification scaffolding
- Media uploads and richer center profiles
- CI pipelines and deployment environments

## Attribution

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.
