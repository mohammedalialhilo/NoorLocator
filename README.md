# NoorLocator

NoorLocator is a moderated platform for discovering Shia centers, browsing majalis, publishing manager-owned center updates, and contributing trusted community information through clear role-based workflows.

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.

## Project Overview

NoorLocator is built around a simple purpose: no follower of Ahlulbayt (AS) should feel disconnected from their community.

The platform focuses on:

- public center discovery with location-aware relevance
- multi-language accessibility for diaspora communities
- community contribution with moderation-first trust controls
- manager-owned operational content for approved centers
- clear identity, mission, and attribution across the product

## Tech Stack

- Frontend: HTML, CSS, JavaScript
- Backend: ASP.NET Core Web API
- Database: MySQL with EF Core and Pomelo
- Authentication: JWT
- Media storage: provider-based local or Azure Blob storage
- Testing: xUnit unit tests and integration tests
- Documentation: Swagger / OpenAPI with XML comments

## Architecture Summary

```text
NoorLocator.sln
|-- NoorLocator.Api
|-- NoorLocator.Application
|-- NoorLocator.Domain
|-- NoorLocator.Infrastructure
|-- frontend
|-- tests
|   |-- NoorLocator.UnitTests
|   `-- NoorLocator.IntegrationTests
`-- scripts
```

- `NoorLocator.Api`
  Hosts controllers, authentication, authorization, Swagger, middleware, and the static frontend.
- `NoorLocator.Application`
  Defines DTOs, interfaces, validation, and shared response models.
- `NoorLocator.Domain`
  Contains entities and enums only.
- `NoorLocator.Infrastructure`
  Implements EF Core persistence, migrations, seed data, auth services, auditing, media storage, and business services.
- `frontend`
  Static branded UI that consumes only the live API.
- `tests`
  Unit and integration coverage for service logic and key HTTP flows.
- `scripts`
  Developer automation such as end-to-end verification.

## Core Capabilities

- Public center discovery with search, nearest-center lookup, distance calculation, languages, images, announcements, and center detail pages
- JWT authentication with `User`, `Manager`, and `Admin` roles
- Centralized auth state with session-backed logout and immediate server-side session invalidation
- Shared self-service profile management for every authenticated user through `profile.html` and `/api/profile/me`
- Unified frontend branding through `frontend/assets/logo_bkg.png` across the shared shell, auth pages, workspace pages, public hero areas, favicon, and web manifest
- User contribution workflows for center requests, suggestions, language suggestions, and manager requests
- Manager workflows for majalis CRUD, event announcements, and center gallery management
- Admin moderation for approvals, rejections, reviews, center management, center-image cleanup, user summaries, and audit logs
- Manifesto-driven identity content through `/about` and `/api/content/about`

## Roles

- `Guest`
  Public visitor. Can browse published centers, languages, majalis, images, announcements, and About content.
- `User`
  Authenticated contributor. Can submit moderated requests and suggestions.
- `Manager`
  Authenticated user approved for one or more centers. Can manage majalis, announcements, and images for assigned centers only.
- `Admin`
  Full moderation and governance access, including approval, rejection, review, delete, audit visibility, and override powers.

## Setup

### Prerequisites

- .NET SDK 10
- MySQL 8.x

### Configuration

Safe shared defaults and placeholders live in:

- `NoorLocator.Api/appsettings.json`
- `NoorLocator.Api/appsettings.Development.json`
- `NoorLocator.Api/appsettings.Production.json`

Do not commit real secrets. Override them with environment variables or user-secrets.

Useful environment variables:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=YOUR_PASSWORD;"
$env:MYSQLCONNSTR_DefaultConnection = "Server=your-server.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=YOUR_USER;Password=YOUR_PASSWORD;"
$env:Jwt__Key = "a-secure-random-string-with-at-least-32-characters"
$env:Cors__AllowedOrigins__0 = "https://your-frontend-host.example"
$env:MediaStorage__RelativeRootPath = "uploads"
$env:Seeding__SeedAdminAccount = "true"
$env:Seeding__AdminEmail = "admin@your-domain.example"
$env:Seeding__AdminPassword = "a-secure-bootstrap-password"
$env:Seeding__SeedDemoData = "false"
```

### Restore And Build

```powershell
cd C:\Users\alhil\Desktop\NoorLocator\NoorLocator
dotnet restore
dotnet build NoorLocator.sln
```

### Database Migration Steps

Recommended production flow:

- run migrations before the app instance starts serving traffic
- keep `Seeding__ApplyMigrations=false` for steady-state production App Service instances
- use `Seeding__SeedAdminAccount=true` only for the first bootstrap when you need an initial admin
- keep `Seeding__SeedDemoData=false` in production

Apply migrations directly with EF Core:

```powershell
dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
```

Apply migrations with the helper script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "Server=your-server.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=YOUR_USER;Password=YOUR_PASSWORD;"
```

Generate an idempotent SQL script for controlled rollouts:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql
```

Create a new migration:

```powershell
dotnet ef migrations add YourMigrationName --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
```

### Run The Application

The frontend is served by the API, so one process starts the whole app:

```powershell
dotnet run --project .\NoorLocator.Api\NoorLocator.Api.csproj
```

Default local endpoints:

- App: `https://localhost:7132/`
- HTTP fallback: `http://localhost:5141/`
- Swagger: `https://localhost:7132/swagger`
- Health: `https://localhost:7132/api/health`
- About page: `https://localhost:7132/about`

## Development Seeded Accounts

These accounts are created by the development seeding defaults:

- Admin bootstrap account, created when `Seeding:SeedAdminAccount=true`: `admin@noorlocator.local` / `Admin123!Pass`
- Manager demo account, created when `Seeding:SeedDemoData=true`: `manager@noorlocator.local` / `Manager123!Pass`
- User demo account, created when `Seeding:SeedDemoData=true`: `user@noorlocator.local` / `User123!Pass`

For production first-run bootstrap:

- set `Seeding__SeedAdminAccount=true`
- provide `Seeding__AdminName`, `Seeding__AdminEmail`, and `Seeding__AdminPassword`
- turn `Seeding__SeedAdminAccount=false` again after the admin account exists

## Development Seeded Demo Data

The demo content below is development-oriented seed data. Production defaults keep demo seeding off unless you intentionally enable it for a non-public environment.

- Languages: Arabic, Swedish, English, Farsi, Urdu
- Published demo centers across Copenhagen, Stockholm, Helsinki, Oslo, and Aarhus
- Demo majalis
- Approved manager assignments
- Public center images
- Published event announcements
- Manifesto-backed site content for the home page and About page

## API Overview

Authentication:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/logout`

Profile:

- `GET /api/profile/me`
- `PUT /api/profile/me`

Public discovery:

- `GET /api/centers`
- `GET /api/centers/{id}`
- `GET /api/centers/nearest`
- `GET /api/centers/search`
- `GET /api/centers/{id}/majalis`
- `GET /api/centers/{id}/languages`
- `GET /api/centers/{id}/images`
- `GET /api/majalis`
- `GET /api/majalis/{id}`
- `GET /api/event-announcements`
- `GET /api/event-announcements/{id}`
- `GET /api/languages`
- `GET /api/content/about`

User contribution:

- `POST /api/center-requests`
- `GET /api/center-requests/my`
- `POST /api/suggestions`
- `POST /api/center-language-suggestions`
- `POST /api/manager/request`

Manager:

- `GET /api/manager/my-centers`
- `POST /api/majalis`
- `PUT /api/majalis/{id}`
- `DELETE /api/majalis/{id}`
- `POST /api/event-announcements`
- `PUT /api/event-announcements/{id}`
- `DELETE /api/event-announcements/{id}`
- `POST /api/center-images/upload`
- `PUT /api/center-images/{id}/set-primary`
- `DELETE /api/center-images/{id}`

Admin:

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

## Swagger

Swagger is enabled in development and can also be enabled by configuration:

- `NoorLocator.Api/Program.cs`
- `NoorLocator.Api/OpenApi/SwaggerDefaultResponsesOperationFilter.cs`
- `NoorLocator.Api/OpenApi/SwaggerSchemaIdFormatter.cs`

It includes:

- grouped XML-comment documentation
- bearer-token security metadata
- standardized error responses
- DTO-based request and response shapes

## Testing

Run all automated tests:

```powershell
dotnet test NoorLocator.sln
```

Current automated coverage:

- `19` unit tests
- `32` integration tests
- `51` passing tests in the current deployment-readiness verification pass

Important test areas:

- auth registration, login, and logout invalidation
- self-service profile read/update, invalid-input rejection, duplicate-email protection, and role protection
- expired-token rejection and refresh-token-backed session revocation
- public discovery endpoints
- admin authorization
- user contribution workflows
- manager majalis workflows
- announcement publishing
- center image validation, ownership enforcement, primary-image changes, deletion, and static file reachability
- admin approval flows

## End-To-End Verification

A reusable live verification script is included:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-e2e.ps1 -StartApp -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=YOUR_PASSWORD;"
```

The script verifies:

- public pages and identity content
- login and logout behavior
- profile read/update behavior for user, manager, and admin accounts
- protected route invalidation after logout
- manager and admin token invalidation after logout
- center discovery endpoints
- user submissions
- manager majalis workflows
- manager announcements and gallery uploads
- invalid image type and oversized upload rejection
- manager-center ownership enforcement for uploads
- primary-image changes plus manager and admin image deletion
- static uploaded image reachability from the public `/uploads` path
- admin approvals and audit access
- public visibility of published content

See `VERIFICATION_REPORT.md` for the final verification summary.

## Frontend And Backend Run Model

- There is no separate frontend dev server required for normal local use.
- The API serves the `frontend/` directory as static assets.
- Protected UI pages are hidden behind a shared auth bootstrap until `/api/auth/me` confirms the active session.
- Logout buttons in the navbar, dashboard, manager workspace, and admin workspace all route through the same frontend logout helper.
- Every authenticated role can open `profile.html` to edit only their own display name and email while keeping role and password fields protected.
- Frontend branding is standardized on `frontend/assets/logo_bkg.png`, with `frontend/js/layout.js` acting as the shared source for navbar, footer, favicon, and page-level logo hydration.
- Manager image uploads use a shared multipart upload helper that resolves the API base URL before sending files, so the upload flow stays aligned with the same live API as the rest of the app.
- Workspace pages, including `profile.html`, are excluded from service-worker precaching and returned with no-store cache headers to reduce stale protected-page restores after logout.
- All real security is still enforced server-side by the API.

## Media Handling

- Local uploads are stored under the configured `MediaStorage__RelativeRootPath` value, which defaults to `uploads`
- Production Azure deployments can switch to Azure Blob Storage with `MediaStorage__Provider=AzureBlob`
- The database stores image URLs only, not binary image data
- `POST /api/center-images/upload` accepts `multipart/form-data`
- Supported formats: `jpg`, `jpeg`, `png`, `webp`
- Max upload size: `5 MB`
- Uploads are validated server-side by presence, extension, size, and file signature
- Files are stored with generated names rather than raw user filenames
- Each uploaded image is linked to a center and the manager who uploaded it
- Managers can upload only for assigned centers, while admins can still delete unsafe or broken images
- Public center details pages display the primary image in the hero area and the remaining gallery images below it
- Azure App Service deployments should prefer `MediaStorage__Provider=AzureBlob`; local storage on App Service is allowed only when you point `MediaStorage__RelativeRootPath` at an absolute writable path under `HOME`

## Docker

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Default compose services:

- API: `http://localhost:8080`
- MySQL: `localhost:3306`

Before using Docker outside local experimentation:

- replace every placeholder in `.env`
- set real `Cors__AllowedOrigins__*` values
- keep the uploads volume persistent
- keep `Seeding__SeedDemoData=false`

## Azure App Service Deployment

Recommended deployment shape:

- Runtime stack: `.NET 10`
- Publish mode: framework-dependent `dotnet publish`
- Startup command: none required for the published NoorLocator API package
- Health Check path: `/api/health/ping`
- Production environment: `ASPNETCORE_ENVIRONMENT=Production`
- Frontend strategy: serve the existing `frontend/` assets from the ASP.NET app; do not create a separate static-site deployment unless the architecture changes later

Build the App Service-ready artifact:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-app-service-api.ps1
```

This produces:

- publish output: `.\artifacts\publish\api`
- zip package: `.\artifacts\packages\noorlocator-api-appservice.zip`

Recommended App Service configuration:

- deploy the published ZIP package instead of the raw repo
- set `WEBSITE_RUN_FROM_PACKAGE=1`
- place the MySQL connection string in the App Service Connection strings blade as `DefaultConnection`, or set `MYSQLCONNSTR_DefaultConnection`
- leave `Frontend__ApiBaseUrl` empty when the frontend is served by the same App Service, or set it to the app origin/app root without `/api`
- keep `frontend/assets/logo_bkg.png` in the published package so the favicon, navbar, hero, workspace, and footer branding all resolve from the same deployed asset
- keep `ReverseProxy__UseForwardedHeaders=true`
- keep `Swagger__Enabled=false`
- keep `Seeding__ApplyMigrations=false` for steady-state production instances
- set `MediaStorage__Provider=AzureBlob` plus the required `AzureBlobStorage__*` settings

If you intentionally deploy source code instead of the published package, set:

- `PROJECT=NoorLocator.Api/NoorLocator.Api.csproj`

Post-deployment smoke test:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-deployed-api.ps1 `
  -BaseUrl https://your-app-name.azurewebsites.net `
  -Origin https://your-frontend-host.example `
  -AdminEmail admin@your-domain.example `
  -AdminPassword YOUR_ADMIN_PASSWORD `
  -ManagerEmail manager@your-domain.example `
  -ManagerPassword YOUR_MANAGER_PASSWORD
```

Frontend smoke test:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-frontend.ps1 `
  -BaseUrl https://your-app-name.azurewebsites.net `
  -ExpectedApiBaseUrl https://your-app-name.azurewebsites.net `
  -UserEmail user@your-domain.example `
  -UserPassword YOUR_USER_PASSWORD `
  -ManagerEmail manager@your-domain.example `
  -ManagerPassword YOUR_MANAGER_PASSWORD `
  -AdminEmail admin@your-domain.example `
  -AdminPassword YOUR_ADMIN_PASSWORD
```

## Production Notes

- Move connection strings and JWT secrets to environment variables or a secret store
- Azure App Service MySQL connection string formats such as `MYSQLCONNSTR_DefaultConnection` and `AZURE_MYSQL_CONNECTIONSTRING` are supported
- Azure MySQL host names ending in `.mysql.database.azure.com` automatically receive `SslMode=Required` if the connection string omits it
- Prefer running `dotnet ef database update` or `scripts/apply-db-migrations.ps1` before app startup instead of enabling automatic migrations on every production boot
- Keep `Seeding__ApplyMigrations=false` in normal production operation
- Turn on `Seeding__SeedReferenceData=true` and `Seeding__SeedAdminAccount=true` only for the first bootstrap when you need reference content and an initial admin, then turn `Seeding__SeedAdminAccount=false` again
- Keep `Seeding__SeedDemoData=false` for production
- Keep `Swagger:Enabled` disabled outside development unless explicitly required
- Configure real `Cors:AllowedOrigins`
- The production frontend is served by the ASP.NET app in the same deployment package
- Leave `Frontend__ApiBaseUrl` empty for same-origin hosting, or set it to the app origin/app root without `/api`
- Set `Frontend__PublicOrigin` to the public frontend origin used for CORS and runtime metadata
- Set `MediaStorage__Provider=AzureBlob` plus the `AzureBlobStorage__*` settings for Azure-hosted media
- If `WEBSITE_RUN_FROM_PACKAGE=1` is enabled on App Service, do not leave `MediaStorage__Provider=Local` on a relative path; use Azure Blob or an absolute writable path under `HOME`
- Keep the centralized session-backed logout flow, profile session-refresh helper, protected-page auth bootstrap, and no-store workspace caching rules intact when modifying auth

## Phase 10 Verification

Phase 10 logout verification was completed against the live MySQL-backed app and the automated test suite.

- `dotnet build NoorLocator.sln`
- `dotnet test NoorLocator.sln --no-build`
- `powershell -ExecutionPolicy Bypass -File .\\scripts\\verify-e2e.ps1 -StartApp -ConnectionString \"Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=...;\"`
- Headless Edge browser verification against the live app confirmed:
  - login succeeds for user, manager, and admin flows
  - logout buttons are visible in the navbar and on dashboard, manager, and admin pages
  - logout clears auth storage and updates the navbar to the logged-out state
  - protected API requests return `401` when replaying the pre-logout token
  - direct navigation, page refresh, and browser back do not restore authenticated workspace access after logout

## Phase 11 Verification

Phase 11 center-image upload verification was completed against the live MySQL-backed app, the automated test suite, and a headless Edge browser probe.

- `dotnet test NoorLocator.sln`
- `powershell -ExecutionPolicy Bypass -File .\\scripts\\verify-e2e.ps1 -StartApp -BaseUrl http://127.0.0.1:5210 -ConnectionString \"Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=...;\"`
- Headless Edge verification against `http://127.0.0.1:5210` confirmed:
  - manager and admin login API calls succeeded before the browser workflows were bootstrapped

## Phase 12 Verification

Phase 12 profile-management verification was completed against the live MySQL-backed app and the automated test suite.

- `dotnet build NoorLocator.sln`
- `dotnet test NoorLocator.sln --no-build`
- `powershell -ExecutionPolicy Bypass -File .\\scripts\\verify-e2e.ps1 -StartApp -BaseUrl http://127.0.0.1:5210 -ConnectionString \"Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=...;\"`
- Verified outcomes:
  - authenticated users can load `/api/profile/me`
  - authenticated users can update their own display name and email through `PUT /api/profile/me`
  - updated profile data persists and is reflected by `/api/auth/me`
  - invalid profile input returns clear `400` errors and duplicate emails return `409`
  - anonymous requests to the profile endpoints return `401`
  - overposted `role` and `passwordHash` fields do not change the stored role or password hash
  - manager and admin accounts can update their own profiles without losing their role or session access
  - the manager gallery UI rejected invalid file types and oversized images with clear messages
  - valid manager uploads completed without the old "can't reach api" failure and refreshed the gallery
  - the public center details page rendered a prominent hero image plus gallery items from uploaded media
  - the admin image moderation section loaded and deleted images successfully

## Future Roadmap

- refresh token rotation with a dedicated refresh endpoint
- multilingual UI scaffolding for English, Arabic, and Swedish
- favorites or saved centers
- calendar-style majalis browsing
- nearby majalis and notification scaffolding
- announcement scheduling, expiry, and pinning
- managed cloud image resizing and compression
- CI/CD pipelines and deployment environments

## Additional Documentation

- `AZURE_RESOURCES.md`
- `DEVELOPER_MANUAL.md`
- `DEPLOYMENT.md`
- `DEPLOYMENT_CHECKLIST.md`
- `VERIFICATION_REPORT.md`
- `scripts/verify-e2e.ps1`

## Attribution

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.
