# NoorLocator

NoorLocator is a moderated web platform for discovering Shia Islamic centers, browsing published majalis, and submitting authenticated community contributions for review.

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.

## Current Scope

The project currently includes:

- public center discovery with search, filtering, nearest-center lookup, and server-side distance calculation
- JWT authentication with `Guest`, `User`, `Manager`, and `Admin` roles
- MySQL persistence through EF Core and Pomelo
- seeded users, languages, demo centers, and sample manager assignment
- moderation-first contribution workflows for authenticated users
- manager-scoped majlis CRUD with center-assignment enforcement
- admin moderation, center management, user visibility, and audit review tools

## Architecture

```text
NoorLocator.sln
|-- NoorLocator.Api
|-- NoorLocator.Application
|-- NoorLocator.Domain
|-- NoorLocator.Infrastructure
|-- frontend
```

- `NoorLocator.Api`: controllers, auth middleware, Swagger, static frontend hosting, startup migration, and seed execution
- `NoorLocator.Application`: DTOs, service interfaces, validation contracts, and shared response models
- `NoorLocator.Domain`: entities and enums for centers, users, majalis, moderation, and audit data
- `NoorLocator.Infrastructure`: EF Core `DbContext`, MySQL provider setup, migrations, seeding, auth services, auditing, and concrete business services
- `frontend`: branded HTML, CSS, and JavaScript pages that consume only the live API

## Implemented Workflows

### Public discovery

- `GET /api/centers`
- `GET /api/centers/{id}`
- `GET /api/centers/nearest?lat={lat}&lng={lng}`
- `GET /api/centers/search?query=&city=&country=&languageCode=`
- `GET /api/centers/{id}/majalis`
- `GET /api/centers/{id}/languages`
- `GET /api/languages`

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`

### Authenticated user contributions

- `POST /api/center-requests`
- `GET /api/center-requests/my`
- `POST /api/suggestions`
- `POST /api/center-language-suggestions`
- `POST /api/manager/request`

All contribution endpoints require authentication. User submissions do not write directly to public center data. Center requests and center language suggestions are stored as `Pending`, manager access requests are approval-based, and suggestions are tracked by type and review status. Audit entries are written for submission and auth-critical events.

### Manager workflows and majalis CRUD

- `GET /api/manager/my-centers`
- `GET /api/majalis?centerId=`
- `GET /api/majalis/{id}`
- `POST /api/majalis`
- `PUT /api/majalis/{id}`
- `DELETE /api/majalis/{id}`

Manager and admin routes enforce server-side authorization. A manager must be assigned to the target center before they can create, edit, move, or delete majalis for it. Languages must come from the predefined `Languages` table, and majlis-language links are persisted through `MajlisLanguages`.

### Admin moderation and management

- `GET /api/admin/dashboard`
- `GET /api/admin/center-requests`
- `POST /api/admin/center-requests/{id}/approve`
- `POST /api/admin/center-requests/{id}/reject`
- `GET /api/admin/manager-requests`
- `POST /api/admin/manager-requests/{id}/approve`
- `POST /api/admin/manager-requests/{id}/reject`
- `GET /api/admin/center-language-suggestions`
- `POST /api/admin/center-language-suggestions/{id}/approve`
- `POST /api/admin/center-language-suggestions/{id}/reject`
- `GET /api/admin/suggestions`
- `PUT /api/admin/suggestions/{id}/review`
- `GET /api/admin/users`
- `GET /api/admin/centers`
- `PUT /api/admin/centers/{id}`
- `DELETE /api/admin/centers/{id}`
- `GET /api/admin/audit-logs`

Admin routes are server-enforced and do not rely on frontend-only moderation rules. Approving a center request creates a live `Center`, approving a manager request creates or updates `CenterManagers`, approving a center language suggestion adds to `CenterLanguages`, and admin approvals, rejections, edits, deletions, and reviews all write audit logs.

## Seeded Accounts

- Admin: `admin@noorlocator.local` / `Admin123!Pass`
- Manager: `manager@noorlocator.local` / `Manager123!Pass`
- User: `user@noorlocator.local` / `User123!Pass`

## Seeded Data

- Languages: Arabic, Swedish, English, Farsi, Urdu
- Demo centers in Copenhagen, Stockholm, and Helsinki
- One approved manager assignment for the seeded manager account
- One sample majlis linked to the Copenhagen demo center

## Database Setup

The app is configured for MySQL.

Default development target:

```text
Server=127.0.0.1;Port=3306;Database=Noorlocator
```

Update these files if needed:

- `NoorLocator.Api/appsettings.json`
- `NoorLocator.Api/appsettings.Development.json`

Make sure the MySQL username, password, and `MySql:ServerVersion` value match your local server.

## Running The App

From the repo root:

```powershell
cd C:\Users\alhil\Desktop\NoorLocator\NoorLocator
dotnet restore
dotnet build NoorLocator.sln
dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
dotnet run --project .\NoorLocator.Api\NoorLocator.Api.csproj
```

Default URLs:

- App: `https://localhost:7132/`
- HTTP fallback: `http://localhost:5141/`
- Swagger: `https://localhost:7132/swagger`
- Health: `https://localhost:7132/api/health`

## Frontend Notes

- The frontend stores auth state in `localStorage`
- The navbar reflects logged-in and role-aware state
- The dashboard now supports center requests, user suggestions, center language suggestions, manager access requests, and request-status tracking
- The manager workspace now shows assigned centers, loads majalis by center, and supports create, edit, and delete actions with language checkbox selection
- The admin workspace now provides moderation queues, center editing/deletion, user visibility, and audit-log review with confirmation-based actions
- Browser geolocation is used for public nearby-center discovery, with search fallback when location is unavailable

## Migrations

Migration files live under:

- `NoorLocator.Infrastructure/Persistence/Migrations`

## Attribution

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.
