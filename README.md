# NoorLocator

NoorLocator is a moderated web platform for discovering Shia Islamic centers, browsing upcoming majalis, publishing manager-led center updates, and contributing trusted community information through role-based workflows.

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.

## Project Purpose & Vision

NoorLocator is grounded in a simple manifesto-driven purpose: no follower of Ahlulbayt (AS) should feel disconnected from their community, no matter where they are in the world.

The project exists to:

- help people find nearby Shia centers
- make majalis and center activities easier to discover
- reduce language barriers in diaspora communities
- empower community contribution without sacrificing trust or authenticity

This is why the platform is location-aware, moderation-first, role-based, and strict about structured language data.

## What Is Included

- Public center discovery with nearest-center lookup, search, filters, gallery images, and center detail pages
- JWT authentication with `Guest`, `User`, `Manager`, and `Admin` roles
- Moderation-first user contribution workflows for center requests, language suggestions, manager requests, and product feedback
- Manager-only majlis CRUD with center assignment enforcement
- Manager-published event announcements with direct publish or draft status
- Manager-owned center gallery upload, primary image management, and secure image validation
- Manifesto-backed About page, identity-focused homepage, and reusable content API for site purpose
- Admin moderation dashboard, center management, user visibility, and audit logs
- Swagger/OpenAPI documentation
- Docker support for the API and MySQL
- Unit and integration tests
- PWA basics with a web manifest and service worker shell caching

## Stack

- Frontend: HTML, CSS, JavaScript
- Backend: ASP.NET Core Web API
- Database: MySQL with EF Core and Pomelo
- Auth: JWT
- Media storage: local file storage for development with a storage abstraction for production providers
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
  Implements EF Core, MySQL setup, migrations, seeding, JWT generation, password hashing, auditing, media storage, and concrete services.
- `frontend`
  Contains the branded static client that consumes only the live API.
- `tests`
  Covers service-level logic and key HTTP flows.

## Roles

- `Guest`
  Unauthenticated public visitor who can browse published centers, public majalis, center images, and published announcements.
- `User`
  Authenticated community member who can submit moderated contributions.
- `Manager`
  Authenticated user assigned to one or more approved centers and allowed to manage majalis, event announcements, and center gallery media there.
- `Admin`
  Full moderation and management access across the platform, including override deletion for announcements and media.

## Seeded Accounts

- Admin: `admin@noorlocator.local` / `Admin123!Pass`
- Manager: `manager@noorlocator.local` / `Manager123!Pass`
- User: `user@noorlocator.local` / `User123!Pass`

## Seeded Demo Data

- Languages: Arabic, Swedish, English, Farsi, Urdu
- Demo centers in Copenhagen, Stockholm, Helsinki, Oslo, and Aarhus
- Approved manager assignments for the seeded manager account
- Multiple public demo majalis across the seeded centers
- Seeded center gallery images and manager-authored announcements for the public discovery experience

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

Phase 8 adds:

- `AddEventAnnouncementsAndCenterImages`

Phase 9 adds:

- `AddAppContentIdentityLayer`

## Media Storage

- Development uploads use local storage under `frontend/uploads`
- Only generated file names are stored and served
- Supported formats: `jpg`, `jpeg`, `png`, `webp`
- Max upload size: `5MB`
- The database stores only image URLs

For production, keep the `IMediaStorageService` abstraction and swap the local implementation for Azure Blob Storage, AWS S3, or another managed provider.

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

Swagger includes:

- XML-comment summaries for the API surface
- JWT bearer security definition
- Default error response documentation
- Documented public, authenticated, manager, and admin endpoints

Main endpoint groups:

- Auth: `/api/auth/*`
- Public discovery: `/api/centers/*`, `/api/languages`, `/api/majalis`, `/api/event-announcements`, `/api/content/about`
- User contributions: `/api/center-requests`, `/api/suggestions`, `/api/center-language-suggestions`, `/api/manager/request`
- Manager: `/api/manager/*`, `/api/majalis/*`, `/api/event-announcements`, `/api/center-images/*`
- Admin: `/api/admin/*`

## Testing

Run all tests:

```powershell
dotnet test NoorLocator.sln
```

Included coverage:

- Unit tests for center discovery, center requests, and local media validation
- Integration tests for registration, protected routes, public center search, admin authorization, announcement visibility, and gallery upload flows

Test projects:

- `tests/NoorLocator.UnitTests`
- `tests/NoorLocator.IntegrationTests`

## Frontend Notes

- The frontend consumes only the live API
- Auth state is stored in `localStorage`
- The navbar updates by role and includes a mobile toggle
- The public About page is available at `/about`
- Toasts, loading states, empty states, and responsive layouts are used across public, user, manager, and admin pages
- Center detail pages now show a primary banner image, gallery grid, and event announcement feed
- The homepage and About page now surface manifesto-backed purpose, mission, principles, and attribution through the content API
- PWA basics are included through `site.webmanifest` and `service-worker.js`

## Developer Notes

- `DEVELOPER_MANUAL.md` explains the product philosophy and intent behind moderation, roles, language structure, and manager-controlled announcements

## Production-Readiness Notes

- Unhandled exceptions are returned through a consistent API response shape
- Validation failures include trace-friendly metadata
- Admin and manager write access are enforced server-side through policy-based authorization and center ownership checks
- DTO-based write endpoints reduce overposting risk
- JWT configuration is validated more strictly outside development
- Open CORS is allowed only in development and testing when origins are not explicitly configured
- Uploaded files are validated by extension and file signature, not MIME type alone
- Published API output includes the static frontend assets for container and publish scenarios

## Future Roadmap

- Refresh token rotation and logout/revocation flows
- Multi-language UI scaffolding for English, Arabic, and Swedish
- Favorites or saved centers
- Calendar-style public majalis browsing
- Nearby majalis and notification scaffolding
- Scheduled announcement publishing, expiry dates, and pinned highlights
- Managed cloud media storage with image resizing and compression
- CI pipelines and deployment environments

## Attribution

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.
