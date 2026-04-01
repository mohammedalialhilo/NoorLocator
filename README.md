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

Important deployment note:

- NoorLocator currently supports MySQL-compatible deployments only.
- In Azure, use Azure Database for MySQL Flexible Server.
- Azure SQL Database / SQL Server is **not** a drop-in option for this codebase today because the EF Core provider, migrations, and connection handling are built for MySQL.

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
- Real email-ownership verification before trusted access is granted to a newly registered account
- Forgot-password and reset-password flows with expiring, single-use reset tokens
- Centralized SMTP-backed email delivery with HTML templates and a development/testing pickup-directory fallback
- Centralized auth state with session-backed logout and immediate server-side session invalidation
- In-app notifications with unread badge counts, read state, and a dedicated notifications page
- Email and in-app event notifications for centers a verified user has visited or followed
- User notification preferences plus center follow/subscription controls
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
$env:Frontend__PublicOrigin = "https://your-frontend-host.example"
$env:MediaStorage__RelativeRootPath = "uploads"
$env:AuthFlow__EmailVerificationTokenLifetimeMinutes = "1440"
$env:AuthFlow__PasswordResetTokenLifetimeMinutes = "60"
$env:AuthFlow__VerifyEmailPath = "verify-email.html"
$env:AuthFlow__ResetPasswordPath = "reset-password.html"
$env:SmtpSettings__Host = "smtp.gmail.com"
$env:SmtpSettings__Port = "587"
$env:SmtpSettings__Username = "noorlocator@gmail.com"
$env:SmtpSettings__Password = "YOUR_GMAIL_APP_PASSWORD"
$env:SmtpSettings__FromEmail = "noorlocator@gmail.com"
$env:SmtpSettings__FromName = "NoorLocator"
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
powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -ConnectionString "Server=your-server.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=YOUR_USER;Password=YOUR_PASSWORD;" -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql
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

- Languages: Arabic, Danish, German, English, Spanish, Farsi, Portuguese, Swedish, and Urdu
- Published demo centers across Copenhagen, Stockholm, Helsinki, Oslo, and Aarhus
- Demo majalis
- Approved manager assignments
- Public center images
- Published event announcements
- Manifesto-backed site content for the home page and About page

## Authentication, Email, And Notifications

### Email Verification

- `POST /api/auth/register` creates users in an unverified state and sends a verification email.
- Trusted login is blocked until `GET /api/auth/verify-email?token=...` succeeds.
- `POST /api/auth/resend-verification-email` issues a fresh token for accounts that still need verification.
- Verification tokens are securely random, expire, and are invalidated after use.
- If a verified user changes their email through `PUT /api/profile/me`, the account becomes unverified again until the new address is confirmed.

### Forgot And Reset Password

- `POST /api/auth/forgot-password` always returns a generic success message so account existence is not leaked.
- `POST /api/auth/reset-password` accepts a token, new password, and confirm password.
- Reset tokens are single-use, expire, and are cleared after a successful reset or expired-token rejection.
- Password reset revokes active refresh-token sessions so older access tokens fail on their next authenticated request.

### Email Delivery Configuration

- SMTP/email configuration is externalized under `SmtpSettings`.
- NoorLocator is configured to send from `noorlocator@gmail.com`.
- Gmail setup uses:
  - `SmtpSettings__Host=smtp.gmail.com`
  - `SmtpSettings__Port=587`
  - `SmtpSettings__Username=noorlocator@gmail.com`
  - `SmtpSettings__Password=<gmail app password>`
- HTML email templates exist for:
  - email verification
  - password reset
  - password changed confirmation
  - new majlis notifications
  - new event announcement notifications
- When SMTP credentials are intentionally absent in development or testing, NoorLocator writes the rendered outbound emails to the configured pickup directory instead of silently dropping them.

### Center Visits, Follows, And Notifications

- Opening a center details page as a verified authenticated user records or refreshes a `UserCenterVisit`.
- Users can explicitly follow a center through `POST /api/centers/{id}/subscribe` and stop following with `DELETE /api/centers/{id}/subscribe`.
- Publishing a new majlis or a published event announcement fans out to verified users who visited or followed that center.
- Delivery respects user-level preferences and center-level follow/subscription preferences.
- In-app notifications are stored in the `Notifications` table and surfaced through the desktop profile dropdown, the mobile drawer account section, and `notifications.html`.
- Email notifications are sent only when the destination email is verified and email delivery is enabled for that scenario.

### Notification Preferences

- `GET /api/profile/me/notification-preferences` returns the current preference set.
- `PUT /api/profile/me/notification-preferences` persists:
  - email notifications
  - in-app notifications
  - majlis notifications
  - event notifications
  - center update notifications
- The profile page remains the settings surface for verification status, notification preferences, and preferred language, while logout lives in the shared profile navigation UI.

## Localization And RTL

### Supported UI Languages

- `en` English
- `ar` Arabic
- `fa` Farsi
- `da` Danish
- `de` German
- `es` Spanish
- `sv` Swedish
- `pt` Portuguese

### Frontend Localization Architecture

- Static UI translations live in `frontend/locales/` as one JSON resource file per language.
- `frontend/js/i18n.js` is the shared runtime that loads the selected locale, merges it with the English fallback, and exposes `t(...)`, `translateMessage(...)`, and selector helpers to the rest of the frontend.
- HTML templates use `data-i18n*` attributes for declarative translation, while `frontend/js/app.js` and `frontend/js/layout.js` call `t(...)` for dynamic UI states.
- The selected language is persisted in `localStorage` under the shared NoorLocator language key so it survives reloads for signed-out users.

### Language Switcher And Preferred Language

- Signed-out users get the language selector directly in the desktop navbar and inside the hamburger drawer on mobile.
- Signed-in users switch language from the desktop profile dropdown, the mobile drawer account section, or the preferred-language form on `profile.html`.
- Changing the language updates the page shell immediately and persists the selection across reloads.
- When a user is signed in, `PUT /api/profile/me/preferred-language` stores the preferred UI language on the user record.
- `frontend/js/i18n.js` also defines the selector flag mapping:
  - `ar` -> Iraq
  - `fa` -> Iran
  - `da` -> Denmark
  - `de` -> Germany
  - `es` -> Spain
  - `sv` -> Sweden
  - `pt` -> Portugal
  - `en` -> UK
- On later visits, NoorLocator resolves the language in this order:
  - signed-in user preferred language
  - saved local browser selection
  - supported browser language
  - English fallback

### RTL Strategy

- Arabic and Farsi are treated as RTL languages by the localization runtime.
- When either language is active, NoorLocator updates the root `html` element with `lang="<code>"` and `dir="rtl"`.
- Shared CSS uses logical properties and RTL-aware selectors so the navbar, hamburger menu, cards, filters, forms, and notifications remain usable without duplicating layouts.

### Center Supported Languages

- The backend returns approved supported languages in both `GET /api/centers` and `GET /api/centers/{id}` responses.
- The centers directory renders those languages as responsive chips on each center card.
- The center details page also renders a dedicated supported-languages section.
- Center filtering remains language-aware through `GET /api/centers/search?languageCode=<code>`.

### Adding A New Language Later

1. Add the new language record to the seed/reference data.
2. Add the language metadata to `frontend/js/i18n.js`.
3. Create `frontend/locales/<code>.json` using `frontend/locales/en.json` as the source shape.
4. Verify whether the new language should be treated as RTL and extend the RTL list if needed.
5. Re-run build, tests, and live browser verification before release.

## API Overview

Authentication:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/verify-email`
- `POST /api/auth/resend-verification-email`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/me`
- `POST /api/auth/logout`

Profile:

- `GET /api/profile/me`
- `PUT /api/profile/me`
- `PUT /api/profile/me/preferred-language`
- `GET /api/profile/me/notification-preferences`
- `PUT /api/profile/me/notification-preferences`

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

Center engagement:

- `POST /api/centers/{id}/visit`
- `POST /api/centers/{id}/subscribe`
- `DELETE /api/centers/{id}/subscribe`
- `GET /api/users/me/subscriptions`

Notifications:

- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

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

- `28` unit tests
- `39` integration tests
- `67` passing tests in the current verification pass

Important test areas:

- auth registration, email verification, unverified-access restrictions, login, and logout invalidation
- forgot-password, reset-password, reset-token expiry, single-use enforcement, and session revocation after reset
- self-service profile read/update, invalid-input rejection, duplicate-email protection, role protection, and reverification on email changes
- expired-token rejection and refresh-token-backed session revocation
- public discovery endpoints
- localization-aware profile preference persistence
- center visit tracking, follow/subscription deduplication, notification preference persistence, and notification read-state updates
- in-app and email notifications for majalis and event announcements
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
- locale switching, persisted language selection, and RTL shell behavior
- login and logout behavior
- register, resend-verification, verify-email, forgot-password, and reset-password behavior
- profile read/update behavior for user, manager, and admin accounts
- preferred language persistence for authenticated users
- protected route invalidation after logout
- manager and admin token invalidation after logout
- center discovery endpoints
- supported-language chips on center cards and center details pages
- language-aware center filtering
- visit tracking, center follow/subscription behavior, notification-bell updates, and mark-read flows
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
- Authenticated desktop navigation uses a profile indicator dropdown for profile, notifications, language, and logout, while mobile uses the same actions inside the hamburger drawer account section.
- Every authenticated role can open `profile.html` to edit only their own display name and email while keeping role and password fields protected.
- Verified users see trusted workspace links plus notifications, while unverified users are directed to `verify-email.html` until ownership is confirmed.
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

GitHub Actions automation for this package-and-deploy flow now lives at:

- `.github/workflows/noorlocator-azure-app-service.yml`
- `CI_CD.md`

GitHub Actions deployment settings required for the `production` environment:

- secret: `AZURE_CLIENT_ID`
- secret: `AZURE_TENANT_ID`
- secret: `AZURE_SUBSCRIPTION_ID`
- variable: `AZURE_WEBAPP_NAME`
- auth model: Azure OpenID Connect / federated credential, not a long-lived publish profile

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

## Custom Domain And HTTPS Readiness

Recommended launch shape:

- use one public HTTPS origin for the bundled frontend and API together, for example `https://www.noorlocator.example`
- let the same deployed ASP.NET app answer both the public pages and `/api/*`
- leave `Frontend__ApiBaseUrl` empty for this same-origin model
- set `Frontend__PublicOrigin` and `Cors__AllowedOrigins__*` only to the real production browser origins

Optional API subdomain shape:

- use a separate API origin such as `https://api.noorlocator.example` only when you intentionally split browser traffic away from the frontend host or front the app with a dedicated reverse proxy path
- when you do this, set `Frontend__ApiBaseUrl=https://api.noorlocator.example`
- keep `Frontend__PublicOrigin=https://www.noorlocator.example`
- lock `Cors__AllowedOrigins__*` to the frontend origins only; do not use wildcards

HTTPS requirements:

- bind a valid certificate to every custom host name before launch
- enable App Service `HTTPS Only`
- keep `ReverseProxy__UseForwardedHeaders=true`
- keep `Https__RedirectionEnabled=true`
- verify that an `http://` request redirects to `https://` and that the final site origin stays stable after login, logout, and file uploads

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
- The current auth model uses bearer tokens from `Authorization` headers and stores them in browser storage through `frontend/js/auth.js`; NoorLocator does not currently depend on authentication cookies for launch
- Because launch auth is token-and-storage based, secure cookie flags do not harden the current JWT storage path; if you migrate auth to cookies later, require `Secure`, `HttpOnly`, `SameSite`, and CSRF review as part of that change
- `ApiExceptionMiddleware` returns a generic `500` message outside development and omits exception type details from the response body
- `/api/health` omits the environment name outside development and testing
- Swagger stays disabled in production unless you explicitly enable it
- NoorLocator does not register a development exception page in production
- Outside development and testing, app startup fails if `Cors:AllowedOrigins` does not contain valid absolute origins

## Production Launch Checklist

Before public launch, verify the hosted application with a real production-style dataset and roles:

- home page loads at `/` with live branding and no mixed-content warnings
- About page loads at `/about` and fetches manifesto content successfully
- registration succeeds from `register.html`
- verification-email delivery, verification-link completion, forgot-password, and reset-password flows succeed for a real accessible inbox or configured SMTP/pickup flow
- login succeeds for user, manager, and admin accounts
- logout returns to the logged-out state and old protected requests return `401`
- `dashboard.html` loads for a normal authenticated user
- `manager.html` loads for a manager account
- `admin.html` loads for an admin account
- a user can submit a center request
- a user can follow a center, receive majlis/event notifications, and control notification preferences from the profile page
- a manager can create, edit, and delete a majlis
- a manager can create and remove an announcement
- a manager can upload an image and the image is publicly reachable
- the uploaded image renders on the public center details page
- an admin can review and approve the moderated records needed for launch
- `powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-deployed-api.ps1 -BaseUrl https://your-host ...`
- `powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-frontend.ps1 -BaseUrl https://your-host ...`
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify-mobile-frontend.ps1 -BaseUrl https://your-host ...` when you want an additional mobile-navigation and responsive-layout browser pass

## Rollback And Recovery

If a deployment fails or the site is unhealthy after release:

- confirm the GitHub Actions package artifact was built from the expected commit and still contains `NoorLocator.Api.dll`, `frontend/index.html`, and `frontend/assets/logo_bkg.png`
- check App Service Log Stream and application startup logs first
- verify `MYSQLCONNSTR_DefaultConnection` or the chosen connection-string override is present and points to the intended database
- verify `Jwt__Key` is present and at least `32` characters
- verify `Frontend__PublicOrigin`, `Frontend__ApiBaseUrl`, and every `Cors__AllowedOrigins__*` entry match the real launch domains exactly, including `https://`
- verify `MediaStorage__Provider=AzureBlob` plus the required `AzureBlobStorage__*` settings, or confirm an explicit writable `HOME` path if local storage is intentionally used
- verify the Azure Blob container is reachable and public if public image URLs are expected
- verify migrations were applied to the production database before the app booted
- validate database health with `GET /api/centers` and an authenticated `GET /api/admin/dashboard`
- validate storage health with a manager image upload followed by a public fetch of the returned image URL
- if rollback is required, redeploy the previous known-good App Service package and re-run the smoke checks before reopening traffic

## Deployment Phase D8 Verification

Deployment Phase D8 production-hardening verification was completed against the repository configuration, automated tests, and App Service packaging flow.

- `dotnet test NoorLocator.sln -c Release`
- `powershell -ExecutionPolicy Bypass -File .\scripts\package-app-service-api.ps1`
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify-app-service-package.ps1`
- Verified outcomes:
  - the current repository state passes `28` unit tests and `39` integration tests for `67/67` total passing tests
  - production exception handling returns a generic `500` payload without detailed exception type leakage
  - production health output omits the environment name
  - production defaults keep forwarded headers enabled, HTTPS redirection enabled, Swagger disabled, automatic migrations disabled, and demo seeding disabled
  - the App Service publish output still contains the bundled frontend and the expected deployment ZIP
  - the verified publish output path is `.\artifacts\publish\api`
  - the verified deployment package path is `.\artifacts\packages\noorlocator-api-appservice.zip`
- Remaining manual/live-host work before launch:
  - bind the real custom domain and certificate in Azure
  - run the deployed smoke scripts against the live production origin
  - validate final CORS, database, and blob-storage settings on the real App Service instance

## Phase 10 Verification

Phase 10 logout verification was completed against the live MySQL-backed app and the automated test suite.

- `dotnet build NoorLocator.sln`
- `dotnet test NoorLocator.sln --no-build`
- `powershell -ExecutionPolicy Bypass -File .\\scripts\\verify-e2e.ps1 -StartApp -ConnectionString \"Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=...;\"`
- Headless Edge browser verification against the live app confirmed:
  - login succeeds for user, manager, and admin flows
  - logout is available from the shared profile navigation UI and returns the UI to the logged-out state
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

## Phase 13 Verification

Phase 13 authentication-and-notification verification was completed against the live MySQL-backed app, the automated test suite, and a headless Edge browser run.

- `dotnet build NoorLocator.sln`
- `dotnet test NoorLocator.sln`
- Verified outcomes:
  - registration creates an unverified account and sends a verification email from the configured `noorlocator@gmail.com` identity
  - unverified login is blocked and resend-verification works
  - verification links complete the trust flow and unlock sign-in
  - forgot-password and reset-password work end to end, and the old password stops working after reset
  - reset-password sends a password-changed confirmation email
  - visiting/following a center leads to majlis and event notifications after privileged publishing
  - the in-app notification dropdown access, notifications page, and mark-read flow work
  - notification preferences persist from the profile page
- Live browser verification against `http://127.0.0.1:5213` confirmed:
  - register, resend verification, verify-email, login, follow-center, notification UI, logout, forgot-password, and reset-password all worked in the real frontend
  - the pickup-directory email flow produced `2` verification emails, `2` notification emails, `1` reset email, and `1` password-changed confirmation for the verified browser scenario
  - the verification run used center `20` on the local seeded dataset and the browser-created user `ui-7d81b955b1@test.local`

## Future Roadmap

- refresh token rotation with a dedicated refresh endpoint
- multilingual UI scaffolding for English, Arabic, and Swedish
- favorites or saved centers
- calendar-style majalis browsing
- nearby majalis and notification scaffolding
- announcement scheduling, expiry, and pinning
- managed cloud image resizing and compression
- deployment slots and staged promotions

## Additional Documentation

- `AZURE_RESOURCES.md`
- `CI_CD.md`
- `DEVELOPER_MANUAL.md`
- `DEPLOYMENT.md`
- `DEPLOYMENT_CHECKLIST.md`
- `NoorLocator_Deployment_Guide.md`
- `NoorLocator_Deployment_Guide.docx`
- `scripts/generate-deployment-guide-docx.py`
- `VERIFICATION_REPORT.md`
- `scripts/verify-e2e.ps1`

## Attribution

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.
