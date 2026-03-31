# Developer Manual

## 1. System Overview

NoorLocator is a clean-architecture web system for:

- discovering Shia centers and majalis
- collecting moderated community contributions
- letting approved managers maintain center-owned content
- giving admins full moderation and governance visibility

The application serves a static frontend from the ASP.NET Core API, uses MySQL through EF Core, and stores authentication state through JWT access tokens backed by server-side session records.

## 2. Philosophy And Intent

NoorLocator is shaped by a manifesto-driven purpose: no follower of Ahlulbayt (AS) should feel disconnected from their community.

This is why the system is:

- moderation-first for public trust
- location-aware for practical usefulness
- strict about structured languages for discoverability
- role-based so responsibility and authority stay clear
- explicit about its mission and attribution across the product

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.

## 3. Architecture Explanation

### `NoorLocator.Api`

- configures DI, auth, authorization, CORS, Swagger, middleware, and static file hosting
- exposes controllers only
- maps `/about` directly to the public About page

### `NoorLocator.Application`

- defines DTOs for all HTTP contracts
- defines interfaces for services
- contains validators and shared result models
- should remain free of persistence and framework-heavy logic

### `NoorLocator.Domain`

- defines entities and enums
- contains no EF-specific service logic

### `NoorLocator.Infrastructure`

- contains `NoorLocatorDbContext`
- contains EF configuration classes and migrations
- implements all service interfaces
- handles hashing, JWT creation, audit logging, seeding, and media storage

### `frontend`

- static HTML, CSS, and JavaScript
- consumes only the live API
- handles UI state, rendering, loading states, and auth-aware navigation
- does not enforce business security

## 4. Folder Structure

```text
NoorLocator/
|-- NoorLocator.Api/
|   |-- Controllers/
|   |-- Extensions/
|   |-- Middleware/
|   `-- OpenApi/
|-- NoorLocator.Application/
|   |-- Admin/
|   |-- Authentication/
|   |-- CenterImages/
|   |-- Centers/
|   |-- Content/
|   |-- EventAnnouncements/
|   |-- Languages/
|   |-- Majalis/
|   |-- Management/
|   |-- Suggestions/
|   `-- Validation/
|-- NoorLocator.Domain/
|   |-- Common/
|   |-- Entities/
|   `-- Enums/
|-- NoorLocator.Infrastructure/
|   |-- Persistence/
|   |   |-- Configurations/
|   |   `-- Migrations/
|   |-- Seeding/
|   |-- Security/
|   `-- Services/
|-- frontend/
|   |-- assets/
|   |-- css/
|   `-- js/
|-- tests/
|   |-- NoorLocator.UnitTests/
|   `-- NoorLocator.IntegrationTests/
`-- scripts/
```

## 5. Data Model Explanation

Primary entities:

- `User`
  Account record with hashed password and role.
- `RefreshToken`
  Stores hashed refresh token, session ID, expiry, and revocation status. This is the server-side session record used for logout invalidation.
- `Center`
  Published public center record.
- `CenterRequest`
  User-submitted request that becomes a `Center` only after admin approval.
- `CenterManager`
  Manager-to-center assignment table.
- `ManagerRequest`
  Moderated request for center manager access.
- `Majlis`
  Published center event maintained by managers.
- `Language`
  Controlled language table for center and majlis metadata.
- `MajlisLanguage`
  Many-to-many bridge between majalis and languages.
- `CenterLanguage`
  Many-to-many bridge between centers and languages.
- `CenterLanguageSuggestion`
  Moderated request to add a language to a center.
- `Suggestion`
  User feedback or platform suggestion with review status.
- `EventAnnouncement`
  Manager-authored announcement separate from majalis.
- `CenterImage`
  Public center gallery image metadata.
- `AuditLog`
  Record of critical moderation, management, and auth events.
- `AppContent`
  Seeded narrative and identity content for About and homepage sections.

## 6. Role System And Authorization Rules

### Guest

- public read-only access
- no write access

### User

- can submit center requests
- can submit language suggestions
- can submit platform suggestions
- can request manager access
- cannot write directly to published centers, majalis, announcements, or images

### Manager

- can manage majalis only for assigned centers
- can publish announcements only for assigned centers
- can upload and manage images only for assigned centers
- cannot moderate admin queues unless also an admin

### Admin

- can approve or reject moderated requests
- can review suggestions
- can manage centers and view audit logs
- can override-delete manager content for platform safety, including center images

## 7. Full API Documentation

### Authentication

- `POST /api/auth/register`
  Registers a new `User` and returns tokens plus current user data.
- `POST /api/auth/login`
  Returns JWT access token, refresh token, expiry, and current user data.
- `GET /api/auth/me`
  Returns the authenticated user profile.
- `POST /api/auth/logout`
  Revokes the current session server-side and is used by the frontend logout flow.

### Profile

- `GET /api/profile/me`
  Returns the authenticated user's self-service profile payload.
- `PUT /api/profile/me`
  Updates only the authenticated user's editable profile fields.
- Allowed editable fields:
  - `Name`
  - `Email`
- Protected fields:
  - `Role`
  - `PasswordHash`
  - any other user record outside the authenticated `UserId`

### Public Discovery

- `GET /api/centers`
  Lists public centers and optionally includes distance when `lat` and `lng` are supplied.
- `GET /api/centers/search`
  Filters by query, city, country, and language code.
- `GET /api/centers/nearest`
  Uses the Haversine calculation in the backend.
- `GET /api/centers/{id}`
  Returns center details, languages, and upcoming majalis.
- `GET /api/centers/{id}/majalis`
- `GET /api/centers/{id}/languages`
- `GET /api/centers/{id}/images`
- `GET /api/majalis`
- `GET /api/majalis/{id}`
- `GET /api/event-announcements`
- `GET /api/event-announcements/{id}`
- `GET /api/languages`
- `GET /api/content/about`

### User Contributions

- `POST /api/center-requests`
- `GET /api/center-requests/my`
- `POST /api/suggestions`
- `POST /api/center-language-suggestions`
- `POST /api/manager/request`

### Manager

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

### Admin

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

Swagger is the live reference for DTO shapes and status codes.

## 8. Business Logic Rules

- Published centers are created through `CenterRequest` approval, not direct public writes.
- Language data must come from the `Languages` table only.
- Center language suggestions are always created as `Pending`.
- Suggestions are created with type and review status.
- Manager requests are moderated before they change role or assignment state.
- Managers can only manage content for assigned centers.
- Event announcements do not require admin approval once created by an authorized manager.
- Only one primary image is allowed per center.
- Deleting a primary center image promotes the most recent remaining image when one exists.
- Admin actions write audit log entries.

### Database Deployment And Seeding

- `NoorLocatorDbContext` uses Pomelo EF Core for MySQL and is compatible with MySQL 8.x and Azure Database for MySQL Flexible Server
- `NoorLocatorDbContextFactory` loads `appsettings.json`, the active environment file, and environment variables so `dotnet ef` follows the same connection rules as the app
- supported production connection inputs are `ConnectionStrings:DefaultConnection`, `MYSQLCONNSTR_DefaultConnection`, and `AZURE_MYSQL_CONNECTIONSTRING`
- Azure MySQL host names ending in `.mysql.database.azure.com` automatically receive `SslMode=Required` when the connection string omits it
- the recommended production flow is:
  - run migrations out of band with `dotnet ef database update` or `scripts/apply-db-migrations.ps1`
  - keep `Seeding__ApplyMigrations=false` for steady-state App Service instances
  - use `Seeding__SeedReferenceData=true` and `Seeding__SeedAdminAccount=true` only for the initial bootstrap when needed
  - provide `Seeding__AdminName`, `Seeding__AdminEmail`, and `Seeding__AdminPassword` when bootstrap admin seeding is enabled
  - turn `Seeding__SeedAdminAccount=false` again after the admin exists
  - keep `Seeding__SeedDemoData=false` in production

## 9. Media Handling

- Local development storage is provider-based and defaults to `MediaStorage:RelativeRootPath`, with center gallery images stored under the `center-images` category
- `IMediaStorageService` is the abstraction point used by center images, majalis images, and event-announcement images
- `LocalMediaStorageService` stores validated image bytes on disk and returns an application-served `/uploads/...` URL
- `AzureBlobStorageService` uploads validated image bytes to Azure Blob Storage and returns a direct blob URL or configured public base URL
- configured local storage roots are created automatically, including nested relative paths such as `.codex-temp/uploads-live-local`
- only URLs are stored in the database
- `POST /api/center-images/upload` accepts `multipart/form-data`
- file presence, extension, size, and signature are validated server-side
- file signatures are checked, not MIME type alone
- file names are generated securely through a shared safe-name helper
- upload size is capped by configuration, and multipart form limits are derived from the same storage settings so the request-size limit does not drift from the validation limit
- uploaded images are linked to both `CenterId` and `UploadedByManagerId`
- managers may upload only for assigned centers, while admins may still delete images for moderation and safety
- public center details pages show the primary image in the hero area and the remaining gallery images in the gallery section
- NoorLocator uses the simpler public-read model for center images in Azure:
  - keep the target blob container publicly readable at the blob level
  - if the app creates the container for you, set `AzureBlobStorage__CreateContainerIfMissing=true` and keep `AzureBlobStorage__UseBlobPublicAccess=true`
  - if you set `AzureBlobStorage__PublicBaseUrl`, point it at the public container root or CDN origin for that container
- signed URLs are not part of the current production design because public center images need stable browser-reachable URLs

## 10. Frontend Responsibilities

- render public pages and dashboards
- call real API endpoints only
- default to the same-origin ASP.NET host in production because the frontend is bundled into the published API artifact
- use `Frontend__ApiBaseUrl` only when the frontend must target a different app origin or app root, and do not append `/api`
- store auth state through the shared `frontend/js/auth.js` helper
- render role-aware navigation
- verify protected pages before rendering them
- expose the shared `profile.html` page to every authenticated role
- refresh stored session user data after profile edits so navbar and workspace labels update immediately
- clear auth state on logout
- upload manager center images through the shared multipart upload helper in `frontend/js/api.js`
- show upload progress, gallery refreshes, and clear validation errors for image uploads
- show loading states, empty states, and friendly errors
- never act as the source of truth for security

### Frontend Branding

- the official frontend logo asset is `frontend/assets/logo_bkg.png`
- `frontend/js/layout.js` is the shared branding source for navbar logo, footer logo, favicon links, and any `[data-brand-logo]` image placeholders
- the published App Service artifact keeps `frontend/assets/logo_bkg.png` alongside the rest of the bundled frontend, so branding should be verified from the deployed site rather than from local relative paths alone
- the logo is intentionally used in:
  - the shared navbar and footer
  - landing and About hero panels
  - login, register, and logout identity panels
  - dashboard, manager, admin, and profile workspace hero panels
  - the center-details fallback hero image when a center does not yet have a primary photo
- keep logo sizing reusable through shared classes in `frontend/css/style.css`, especially `.site-logo`, `.site-logo--nav`, `.site-logo--footer`, `.site-logo--auth`, `.site-logo--hero`, `.site-logo--workspace`, and `.site-logo--detail`
- when replacing the brand in the future:
  - replace `frontend/assets/logo_bkg.png` with a new image that keeps a safe aspect ratio
  - review `frontend/js/layout.js`, `frontend/site.webmanifest`, and `frontend/service-worker.js` together so the shell, favicon, manifest, and cached asset list stay aligned
  - verify all public, auth, and protected pages render the updated logo without broken images or layout shifts

## 11. Authentication, Profile, And Logout

### How Login Works

- the client posts credentials to `POST /api/auth/login`
- the API validates the password hash
- the API creates:
  - a JWT access token
  - a refresh token
  - a server-side `RefreshToken` row with a generated `SessionId`
- the JWT includes a `sid` claim that matches the stored session

### How JWT Is Stored

- the frontend stores auth state in `localStorage`
- logout cleanup removes the same keys from both `localStorage` and `sessionStorage`
- logout cleanup also clears matching auth cookies if they ever exist on the frontend origin
- the frontend also stores the current user profile for role-aware navigation
- the current in-memory auth state is reloaded from storage and cleared through one shared helper

### How Profile Editing Works

- every authenticated role uses the same self-service page: `frontend/profile.html`
- the page reads `GET /api/profile/me`
- the page updates `PUT /api/profile/me`
- the backend resolves the authenticated `UserId` from the JWT and never accepts a target user id from the client
- editable fields are limited to `Name` and `Email`
- `Role` and `PasswordHash` are not exposed as writable DTO fields, which prevents direct role escalation or password-hash overposting
- email changes are normalized to lowercase and checked for uniqueness before save
- after a successful save, the frontend updates the cached session user through `updateSessionUser()` so navbar and dashboard labels refresh without forcing logout

### How Protected Pages Are Verified

- `dashboard.html`, `profile.html`, `manager.html`, and `admin.html` declare their auth requirements through `data-auth-*` attributes
- `frontend/js/auth.js` runs `bootstrapPageAuth()` on page load before the workspace initializes
- the page stays behind an auth gate until `GET /api/auth/me` confirms the active session
- manager and admin pages also verify required roles before rendering
- `pageshow` and `storage` listeners re-run the same auth bootstrap so browser back and cross-tab logout do not restore protected UI state incorrectly

### How Logout Works

- every logout entry point calls the shared frontend `logout()` helper
- the frontend calls `POST /api/auth/logout`
- the API revokes the active `RefreshToken` session for the current `sid`
- JWT bearer validation checks that the session record is still active
- once the session is revoked, the old JWT immediately fails with `401`
- the frontend then clears cached auth state, cached workspace shell artifacts, and redirects to the login page with success feedback
- protected workspace HTML is served with no-store headers and excluded from service-worker precaching to reduce stale protected-page restores after logout

### Common Mistakes

- clearing browser storage without revoking the server-side session
- leaving multiple logout implementations that drift out of sync
- assuming a role change in the database updates an already-issued JWT
- letting profile update DTOs bind directly to the `User` entity
- allowing `Role` or `PasswordHash` to flow through the self-service profile endpoint
- trusting client-side route guards as security controls
- adding new protected endpoints without policy or role attributes

### Security Considerations

- keep JWT keys and DB credentials out of committed config
- preserve the session-backed `sid` validation in `Program.cs`
- do not bypass DTOs with direct entity binding
- keep invalid and revoked tokens returning `401`
- keep protected workspace pages non-cacheable after logout-sensitive changes
- review any future refresh-token implementation carefully so it preserves revocation guarantees

### Production Hosting And Hardening

- the launch-default hosting model is same-origin: one HTTPS site serves the frontend and the API together
- the recommended public shape is `https://www.noorlocator.example` or the chosen apex/root domain, with `/api/*` served by the same deployed app
- in that same-origin model, leave `Frontend__ApiBaseUrl` empty and keep `Frontend__PublicOrigin` aligned with the public browser origin
- an optional API subdomain such as `https://api.noorlocator.example` is supported only when you intentionally split origins; if you do that, set `Frontend__ApiBaseUrl` to the API origin and restrict `Cors__AllowedOrigins__*` to the frontend origins only
- production custom domains are HTTPS-only; bind certificates before launch, enable App Service `HTTPS Only`, keep `ReverseProxy__UseForwardedHeaders=true`, and keep `Https__RedirectionEnabled=true`
- outside development and testing, NoorLocator startup fails if `Cors:AllowedOrigins` does not contain valid absolute origins, which prevents a production app from silently running with open CORS
- the app does not register `UseDeveloperExceptionPage`; unhandled exceptions flow through `ApiExceptionMiddleware`, which returns a generic message outside development
- `/api/health` intentionally omits the environment name outside development and testing
- Swagger stays off in production unless explicitly enabled
- launch authentication uses bearer tokens in the `Authorization` header and stores them in browser storage, not in an HttpOnly auth cookie
- the frontend still clears matching cookies opportunistically on logout, but those cookies are not the source of truth for authentication
- if the auth model ever changes to cookies, the replacement must require `Secure`, `HttpOnly`, `SameSite`, and CSRF review before release

## 12. How To Extend The System

- add new DTOs and interfaces in `Application`
- add domain entities or enum changes in `Domain`
- add EF mappings and migrations in `Infrastructure`
- keep controllers thin in `Api`
- wire new frontend features through `frontend/js/api.js`
- add tests in both unit and integration suites for any new critical flow

## 13. Common Pitfalls

- using committed secrets instead of environment overrides
- changing route names without updating the frontend API client
- forgetting to add new entity configuration classes to EF conventions
- testing manager flows with a user token that has not been reissued after role approval
- forgetting that static protected pages still need API-backed auth bootstrap before render
- assuming service worker behavior in local development; NoorLocator disables caching on localhost to reduce stale-auth issues
- leaving `Seeding__SeedAdminAccount=true` permanently after the first production bootstrap
- relying on app-start automatic migrations for every production instance instead of a controlled migration step

## 14. Working Verification

### Automated

- `dotnet build NoorLocator.sln`
- `dotnet test NoorLocator.sln`
- current result during the Deployment Phase D8 pass: `60/60` tests passing
- current production hardening coverage includes:
  - production exception payload redaction
  - production health payload environment redaction
  - production `appsettings.Production.json` defaults for forwarded headers, HTTPS redirection, Swagger disablement, migration disablement, demo-seeding disablement, and Azure Blob preference

### Live Runtime Verification

- `scripts/verify-e2e.ps1` was executed against the real MySQL-backed application
- `dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj` was executed against a fresh scratch MySQL database
- `powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "Server=...;Database=...;User=...;Password=...;"` was executed against a second fresh scratch MySQL database
- `powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql` generated an idempotent SQL migration script
- a production-style API run using `MYSQLCONNSTR_DefaultConnection` booted successfully after migrations and seeded reference data, bootstrap admin data, and optional demo data by configuration
- a live API run using local storage uploaded a manager center image successfully, returned an `/uploads/center-images/...` URL, and served that image back publicly
- the Azure Blob provider path was verified through automated endpoint tests against a local fake blob endpoint:
  - manager upload returned a blob-backed public URL
  - the uploaded bytes were publicly reachable from that returned URL
  - invalid uploads were rejected before any blob write was attempted
  - deleting the image removed the backing blob
- verified flows:
  - registration
  - login
  - self-service profile read and update for authenticated users
  - manager and admin self-profile edits without role changes
  - logout and token invalidation
  - manager and admin logout invalidation
  - public center browsing
  - nearest centers lookup
  - center detail endpoints
  - center request submission
  - suggestion submission
  - center language suggestion submission
  - manager request submission
  - manager majlis create, update, delete
  - manager announcement creation
  - manager image upload reachability
  - invalid image type and oversized upload rejection
  - manager-center ownership enforcement for uploads
  - primary image selection plus manager and admin image deletion
  - static uploaded image reachability
  - admin approvals and suggestion review
  - public visibility of published content
  - About content API and public pages
  - `/api/languages` returned the seeded languages after the production-style bootstrap
  - `/api/centers` returned the seeded demo centers when `Seeding__SeedDemoData=true`
  - admin, manager, and user login all succeeded against the migrated scratch database
- additional browser verification was performed against the live app in headless Edge to confirm:
  - manager and admin login API calls succeed before protected browser workflows are exercised
  - the bundled frontend works against the production-style hosted API configuration without hardcoded localhost URLs
  - the shared `frontend/assets/logo_bkg.png` asset renders in public, auth, and protected pages without broken image links
  - the manager gallery UI shows clear errors for invalid file types and oversized images
  - valid manager uploads complete without the old API-reachability failure and refresh the gallery
  - the public center details page renders a hero image and gallery from uploaded media
  - the admin image moderation section can load and delete gallery items successfully

Future developers should rerun:

```powershell
dotnet test NoorLocator.sln
powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=...;Password=...;"
powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql
powershell -ExecutionPolicy Bypass -File .\scripts\verify-e2e.ps1 -StartApp -BaseUrl http://127.0.0.1:5210 -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=...;Password=...;"
```

### Production Launch Smoke Checklist

- confirm the home page, About page, login page, register page, and logout page all load from the custom production origin without mixed-content warnings
- verify register, login, logout, and post-logout `401` behavior for a normal user
- verify `dashboard.html`, `manager.html`, and `admin.html` only open for the correct authenticated roles
- verify a user can submit a center request
- verify a manager can create a majlis, create an announcement, upload an image, and see that image rendered on the public center details page
- verify an admin can approve the launch-critical moderated records and open the admin dashboard successfully
- verify `scripts/smoke-test-deployed-api.ps1` passes against the deployed host
- verify `scripts/smoke-test-frontend.ps1` passes against the deployed host

### Rollback And Recovery Notes

- when a release fails, inspect App Service startup logs and the GitHub Actions package artifact before changing code
- the most common bad settings are:
  - missing or wrong MySQL connection string
  - missing or too-short `Jwt__Key`
  - mismatched `Frontend__PublicOrigin`
  - incorrect `Frontend__ApiBaseUrl`
  - incorrect `Cors__AllowedOrigins__*`
  - incomplete `AzureBlobStorage__*` settings
  - forgotten `WEBSITE_RUN_FROM_PACKAGE=1`
- validate database configuration with `GET /api/centers`, `GET /api/health`, and an authenticated `GET /api/admin/dashboard`
- validate storage configuration with a real manager upload plus a public fetch of the returned image URL
- if rollback is required, redeploy the previous known-good package and repeat the smoke checklist before reopening launch traffic

## 15. Attribution

Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.
