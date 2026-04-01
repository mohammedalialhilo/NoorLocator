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
  Account record with hashed password, role, email-verification state, verification/reset token hashes, and audit timestamps.
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
- `UserCenterVisit`
  Tracks when a verified authenticated user opens a center details page.
- `UserCenterSubscription`
  Tracks explicit follow/subscription preferences for a center.
- `UserNotificationPreference`
  Stores the authenticated user's email/app/majlis/event/center-update preference switches.
- `Notification`
  Stores in-app notifications, unread state, related entity links, and whether email delivery also occurred.
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
  Registers a new `User`, stores them as unverified, and sends a verification email.
- `POST /api/auth/login`
  Returns JWT access token, refresh token, expiry, and current user data for verified accounts only.
- `GET /api/auth/verify-email`
  Consumes a secure verification token and marks the user email as verified.
- `POST /api/auth/resend-verification-email`
  Sends a fresh verification email for an account that still requires verification.
- `POST /api/auth/forgot-password`
  Starts the reset flow without revealing whether the email exists.
- `POST /api/auth/reset-password`
  Consumes a secure reset token, changes the password, and revokes active sessions.
- `GET /api/auth/me`
  Returns the authenticated user profile.
- `POST /api/auth/logout`
  Revokes the current session server-side and is used by the frontend logout flow.

### Profile

- `GET /api/profile/me`
  Returns the authenticated user's self-service profile payload.
- `PUT /api/profile/me`
  Updates only the authenticated user's editable profile fields.
- `PUT /api/profile/me/preferred-language`
  Updates the authenticated user's preferred UI language.
- `GET /api/profile/me/notification-preferences`
  Returns the authenticated user's notification preference payload.
- `PUT /api/profile/me/notification-preferences`
  Updates the authenticated user's notification preference payload.
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

### Center Engagement

- `POST /api/centers/{id}/visit`
  Records or refreshes a verified user's center-visit record.
- `POST /api/centers/{id}/subscribe`
  Creates or reuses a center follow/subscription record.
- `DELETE /api/centers/{id}/subscribe`
  Removes the authenticated user's center follow/subscription record.
- `GET /api/users/me/subscriptions`
  Returns the authenticated user's current center subscriptions.

### Notifications

- `GET /api/notifications`
  Returns the authenticated verified user's in-app notifications.
- `GET /api/notifications/unread-count`
  Returns the unread notification count used by the shared profile-navigation badges.
- `PUT /api/notifications/{id}/read`
  Marks a single notification as read.
- `PUT /api/notifications/read-all`
  Marks every unread notification for the current user as read.

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
- `GET /api/admin/users/{id}`
- `PUT /api/admin/users/{id}`
- `DELETE /api/admin/users/{id}`
- `GET /api/admin/manager-assignments`
- `POST /api/admin/manager-assignments`
- `PUT /api/admin/manager-assignments/{id}`
- `DELETE /api/admin/manager-assignments/{id}`
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
- Azure SQL Database / SQL Server is not part of the current NoorLocator deployment target; using it would require a provider and migration strategy change
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
- keep public page links and manifest routes relative, such as `index.html`, `about.html`, and `centers.html`, so the same bundle works in App Service and Capacitor-packaged mobile shells
- use `Frontend__ApiBaseUrl` only when the frontend must target a different app origin or app root, and do not append `/api`
- store auth state through the shared `frontend/js/auth.js` helper
- render role-aware navigation
- render verification-aware navigation and block trusted workspace links for unverified accounts
- keep the logged-out language selector in the shared navbar shell
- route signed-in account actions through the shared profile navigation surface instead of detached top-level controls
- verify protected pages before rendering them
- expose the shared `profile.html` page to every authenticated role
- expose `verify-email.html`, `forgot-password.html`, `reset-password.html`, and `notifications.html` through the same shared shell
- refresh stored session user data after profile edits so navbar and workspace labels update immediately
- refresh unread notification counts after auth changes and notification state changes
- clear auth state on logout
- keep desktop account actions behind the `>1050px` profile-indicator dropdown
- keep the same profile, notifications, language, and logout actions available inside the mobile drawer account section at `<=1050px`
- upload manager center images through the shared multipart upload helper in `frontend/js/api.js`
- show upload progress, gallery refreshes, and clear validation errors for image uploads
- keep `admin.html` split into:
  - moderation queues and center/image maintenance
  - a searchable full-width users list with direct row actions
  - a slide-over selected-user editor instead of a permanent side panel
  - manager assignment editing
  - a selected-user ownership view for managed centers, majalis, and announcements
- show loading states, empty states, and friendly errors
- never act as the source of truth for security
- the source frontend lives in `frontend/`, and `NoorLocator.Api/NoorLocator.Api.csproj` copies it into the published API artifact under `frontend/...`, so deployment and Capacitor packaging checks should inspect `artifacts/publish/api/frontend`

### Localization, Locale Files, And RTL

- the frontend localization source of truth is `frontend/locales/`
- the supported UI locales are:
  - `en`
  - `ar`
  - `fa`
  - `da`
  - `de`
  - `es`
  - `sv`
  - `pt`
- `frontend/js/i18n.js` loads the active locale, merges it with `frontend/locales/en.json`, and exposes the shared translation helpers used across the app shell
- HTML templates should prefer `data-i18n`, `data-i18n-placeholder`, `data-i18n-title`, and related attributes over hardcoded text
- JavaScript-rendered UI should prefer `t(...)` for keyed labels and the shared message helpers for runtime status copy
- locale resolution happens in this order:
  - authenticated user's `PreferredLanguageCode`
  - saved browser selection in `localStorage`
  - supported browser language
  - English fallback
- signed-out users access the language switcher from the shared navbar:
  - inline in the desktop navbar for widths above `1050px`
  - inside the hamburger drawer on widths at or below `1050px`
- signed-in users access language controls from profile-related UI:
  - the shared profile dropdown on desktop
  - the shared profile/account section inside the mobile drawer
  - the preferred-language form on `profile.html`
- when users save a preferred language from `profile.html`, the frontend calls `PUT /api/profile/me/preferred-language`
- `SupportedLanguageCatalog` in `NoorLocator.Application/Common/Localization/SupportedLanguageCatalog.cs` is the backend source of truth for allowed preferred-language codes
- the shared frontend language metadata in `frontend/js/i18n.js` also defines the visual flag mapping used in selectors:
  - `ar` -> Iraq flag
  - `fa` -> Iran flag
  - `da` -> Denmark flag
  - `de` -> Germany flag
  - `es` -> Spain flag
  - `sv` -> Sweden flag
  - `pt` -> Portugal flag
  - `en` -> UK flag
- Arabic and Farsi are the RTL locales
- when RTL is active, `frontend/js/i18n.js` updates `document.documentElement.lang`, `document.documentElement.dir`, and the shared body RTL class
- `frontend/css/style.css` uses logical properties and RTL-aware rules so the navbar, hamburger drawer, cards, badges, forms, filters, and notifications keep the same layout model in both directions
- if you add a new locale later:
  - add its metadata in `i18n.js`
  - add `frontend/locales/<code>.json`
  - decide whether it belongs in the RTL set
  - verify public pages, auth pages, dashboards, and the centers flow again

### Center Supported Languages And Filtering

- `Language`, `CenterLanguage`, and `CenterLanguageSuggestion` remain the controlled language model
- `NoorLocatorDbInitializer` seeds the required localization languages plus demo center-language assignments for development verification
- `CenterSummaryDto` and `CenterDetailsDto` include approved center languages so the frontend can render them directly
- the centers directory renders supported-language chips on every center card
- the center details page renders the same approved languages in its details layout
- manager and admin language selectors use the same flag-plus-label metadata as the navbar/profile selectors
- admin moderation rows and ownership views should render preferred and supported languages with both flag and text, never flag-only
- center filtering stays server-driven through `GET /api/centers/search` with `languageCode`
- admin approval still gates language suggestions before they become public

### Admin User Management And Manager Assignments

- the admin workspace is the only frontend surface for global user management
- the backend is the only source of truth for authorization:
  - every `/api/admin/*` user-management or assignment endpoint requires the `AdminArea` policy
  - managers and normal users must never be trusted based on hidden buttons or client routing alone
- `GET /api/admin/users` is the searchable summary list and exposes role, preferred language, verification, assigned-center counts, and whether safe deletion is currently allowed
- the admin users list is intentionally action-oriented:
  - rows show only the highest-signal fields needed for scanning: name, email, role, email status, preferred language, and assigned-center count
  - the permanent side-by-side selected-user panel was removed so the list can use the full workspace width
  - each row now exposes direct `Edit` and `Delete` actions
  - the edit flow opens the shared user editor in a slide-over drawer
  - the delete flow calls the real admin delete endpoint and refreshes the list immediately after success
- protected or system-sensitive accounts stay visible in the list but must not expose an unsafe delete action:
  - the row delete button is disabled when `CanDelete` is false
  - the UI should keep the protection reason available through the disabled-button tooltip/title and the drawer summary note
- `GET /api/admin/users/{id}` returns the selected-user detail payload for:
  - editable account fields
  - notification-preference visibility
  - approved managed centers
  - majalis created by that user
  - event announcements created by that user
- `PUT /api/admin/users/{id}` is intentionally overposting-safe:
  - admins may edit only name, email, role, and preferred language
  - duplicate emails are rejected
  - unsupported language codes are normalized through `SupportedLanguageCatalog`
  - an admin cannot remove their own active-session admin access
  - the last remaining admin cannot be demoted
- `DELETE /api/admin/users/{id}` is guarded rather than blindly destructive:
  - self-delete is blocked
  - deleting the last admin is blocked
  - deleting users who still own majalis, announcements, uploaded images, or personal audit logs is blocked
  - if deletion is allowed, dependent requests/suggestions/assignments are removed first
- approved `CenterManager` rows are the source of truth for manager scope
- admin assignment rules are:
  - creating an approved assignment can promote a `User` account to `Manager`
  - updating an assignment can move that scope to another center or another eligible non-admin user
  - removing the final approved assignment downgrades a manager back to `User`
- manager center access, majlis CRUD, event-announcement CRUD, and center-image upload permissions all flow through the same approved-assignment check, so changing assignments immediately changes publishing scope after the next authenticated request
- the selected-user ownership sections below the users list still depend on the current selected user:
  - opening a row editor also refreshes the ownership view for that user
  - closing the drawer should not clear the selected-user ownership context unless the selected account is deleted

### Responsive Strategy And Mobile Navigation

- `frontend/css/style.css` is the shared responsive layer for public pages, forms, dashboards, and admin tables
- the current breakpoints are:
  - `960px` for collapsing hero, detail, and split-panel layouts into a single column
  - `1050px` for switching the shared navbar into the hamburger-driven mobile menu
  - `720px` for stacking button rows, filter grids, dashboard navigation, and table content for narrow phones
- mobile safety rules in the shared CSS include:
  - `overflow-x: hidden` on the page shell
  - `max-width: 100%` on major content containers and inputs
  - `overflow-wrap: anywhere` on card and table content so long email addresses, metadata, or tokens do not force horizontal scrolling
  - `scroll-margin-top` on section anchors so sticky-header navigation remains usable on smaller screens
  - `prefers-reduced-motion` support so the menu and cards remain comfortable for reduced-motion users
- the hamburger menu is owned by `frontend/js/layout.js` and `frontend/css/style.css`
- desktop navigation rules above `1050px`:
  - primary links stay visible in the navbar
  - authenticated users open profile, notifications, language, and logout from the profile indicator dropdown
  - the unread notification badge is rendered on the profile indicator and the notifications entry inside the dropdown
- mobile navigation rules at or below `1050px`:
  - the hamburger drawer is the single navigation surface
  - signed-out users keep the language selector inside the drawer
  - signed-in users get a dedicated account section inside the drawer with profile, notifications, language, and logout
- standalone authenticated navbar controls for language and notifications are intentionally removed on desktop so the profile indicator is the single source of truth for personal actions
- on screens at or below `1050px`, `layout.js`:
  - renders the toggle button and drawer panel
  - opens and closes the drawer by toggling `aria-expanded`, `.is-open`, `.is-visible`, and scroll-lock state only
  - closes the menu when the scrim is tapped, when a navigation item is selected, when the user presses `Escape`, and when the viewport changes
  - rebuilds the menu after auth changes so logged-in and logged-out states stay correct without a manual refresh
- on the CSS side, the hamburger experience depends on:
  - `.site-nav-toggle`
  - `.site-nav-toggle__line`
  - `.site-header__panel`
  - `.site-nav-scrim`
  - `.site-nav__link`
  - `.utility-row--panel`
- maintain the mobile navbar by editing `buildNavigation()` and `renderHeader()` in `frontend/js/layout.js` first, then keeping the selectors above aligned in `frontend/css/style.css`
- if you add a new top-level page:
  - add the relative route in `buildNavigation()` if it belongs in the primary menu
  - decide whether it should appear for anonymous users, authenticated users, managers, or admins
  - verify its active-link state and drawer close behavior in both desktop and mobile layouts
- the admin users list is no longer a generic data table:
  - desktop uses a three-part row layout for identity, account snapshot badges, and actions
  - narrower layouts collapse the same rows into stacked cards while keeping `Edit` and `Delete` visible
  - the slide-over user editor expands to full width on small screens instead of preserving a cramped side panel
- other admin data tables still become stacked card-like rows on small screens, so future table cells must keep `data-label` attributes when new columns are added
- `frontend/js/config.js` keeps the API base URL configurable through runtime config, page config, same-origin fallback, and remembered overrides
- `frontend/js/layout.js` only registers the service worker on `http` and `https` origins, which avoids breaking `file://` or Capacitor-style packaged environments
- `frontend/site.webmanifest` keeps `id`, `start_url`, and `scope` relative so installable and packaged builds do not assume a root-domain deployment
- `frontend/service-worker.js` precaches only relative shell assets and avoids workspace pages plus runtime config so authenticated/mobile shells do not serve stale protected UI

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

## 11. Authentication, Verification, Password Reset, Notifications, Profile, And Logout

### How Login Works

- the client posts credentials to `POST /api/auth/login`
- the API validates the password hash
- the API blocks trusted login when `IsEmailVerified=false`
- the API creates:
  - a JWT access token
  - a refresh token
  - a server-side `RefreshToken` row with a generated `SessionId`
- the JWT includes a `sid` claim that matches the stored session

### How Email Verification Works

- registration always creates the user with `IsEmailVerified=false`
- the backend generates a secure random verification token, stores only its hash, and stores an expiry timestamp
- the email service sends a verification email from the configured `noorlocator@gmail.com` sender identity
- `GET /api/auth/verify-email?token=...` consumes the token, marks the account verified, and clears the stored token hash and expiry
- `POST /api/auth/resend-verification-email` issues a fresh token when the account still needs verification
- changing the email address through `PUT /api/profile/me` returns the account to the unverified state until the new address is confirmed
- verified-only policies check the current database state, not just the JWT payload, so access shrinks immediately after an email change

### How Password Reset Works

- `POST /api/auth/forgot-password` always returns a generic success message
- when the email exists, the backend generates a secure random reset token, stores only its hash, and stores an expiry timestamp
- the email service sends the reset link from the configured `noorlocator@gmail.com` sender identity
- `POST /api/auth/reset-password` validates the token, expiry, and password confirmation before changing the hash
- a successful password reset clears the reset token and revokes active refresh-token sessions so older access tokens fail on their next authenticated request
- reset tokens are single-use, and expired tokens are cleared when they are rejected

### AuthFlow And SMTP Configuration

- `AuthFlow:EmailVerificationTokenLifetimeMinutes` controls verification-token expiry
- `AuthFlow:PasswordResetTokenLifetimeMinutes` controls reset-token expiry
- `AuthFlow:VerifyEmailPath` and `AuthFlow:ResetPasswordPath` control the frontend routes used in emails
- `SmtpSettings:Host`, `Port`, `Username`, `Password`, `FromEmail`, and `FromName` define the email transport
- NoorLocator is configured to send from `noorlocator@gmail.com`
- development and testing can use `SmtpSettings:WriteToPickupDirectoryWhenDisabled=true` so email templates are still rendered and verified without live SMTP credentials

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
- the page updates preferred UI language through `PUT /api/profile/me/preferred-language`
- the page also reads and writes `GET/PUT /api/profile/me/notification-preferences`
- the backend resolves the authenticated `UserId` from the JWT and never accepts a target user id from the client
- editable fields are limited to `Name` and `Email`
- `Role` and `PasswordHash` are not exposed as writable DTO fields, which prevents direct role escalation or password-hash overposting
- email changes are normalized to lowercase and checked for uniqueness before save
- after an email change, the account becomes unverified again and a new verification email is sent
- after a successful save, the frontend updates the cached session user through `updateSessionUser()` so navbar and dashboard labels refresh without forcing logout
- after a preferred-language save, the frontend updates the cached user and reapplies the shared shell in the selected locale
- the profile page remains the authenticated settings surface for profile edits, notification preferences, and preferred language, while logout now lives in the shared profile navigation menu

### How Protected Pages Are Verified

- `dashboard.html`, `profile.html`, `manager.html`, and `admin.html` declare their auth requirements through `data-auth-*` attributes
- `frontend/js/auth.js` runs `bootstrapPageAuth()` on page load before the workspace initializes
- the page stays behind an auth gate until `GET /api/auth/me` confirms the active session
- verified-only pages and endpoints redirect or return `403` when the account exists but the email is not currently verified
- manager and admin pages also verify required roles before rendering
- `pageshow` and `storage` listeners re-run the same auth bootstrap so browser back and cross-tab logout do not restore protected UI state incorrectly

### How Notifications Work

- visiting a center detail page records or refreshes a `UserCenterVisit` for verified authenticated users
- explicitly following a center creates a `UserCenterSubscription`
- when a manager/admin publishes a new majlis or a published event announcement, the notification service finds verified users who visited or followed that center
- in-app notifications are stored in `Notifications` with unread state, related entity metadata, optional link URLs, and email-delivery flags
- notification delivery is filtered through:
  - user-level preferences in `UserNotificationPreference`
  - center-level follow preferences in `UserCenterSubscription`
  - verified-email status for email delivery
- the frontend surfaces notifications through:
  - the shared profile indicator unread badge and profile-dropdown notifications entry on desktop
  - the shared account section inside the hamburger drawer on mobile
  - `notifications.html`
  - profile notification preference controls
- `PUT /api/notifications/{id}/read` and `PUT /api/notifications/read-all` keep the unread badge and notifications page in sync

### How Logout Works

- desktop logout lives in the shared profile dropdown for widths above `1050px`
- mobile logout lives in the shared account section inside the hamburger drawer
- the frontend calls the shared `logout()` helper
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
- treating email-format validation as proof of email ownership
- storing raw verification or reset tokens instead of hashes
- letting profile update DTOs bind directly to the `User` entity
- allowing `Role` or `PasswordHash` to flow through the self-service profile endpoint
- trusting client-side route guards as security controls
- adding new protected endpoints without policy or role attributes

### Security Considerations

- keep JWT keys and DB credentials out of committed config
- preserve the session-backed `sid` validation in `Program.cs`
- keep verification/reset tokens securely random, hashed at rest, expiring, and single-use
- do not log raw passwords, raw reset tokens, or raw verification tokens
- do not bypass DTOs with direct entity binding
- keep invalid and revoked tokens returning `401`
- keep forgot-password responses generic so account existence is not leaked
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
- current result in the latest auth-and-notification pass: `67/67` tests passing
- current production hardening coverage includes:
  - production exception payload redaction
  - production health payload environment redaction
  - production `appsettings.Production.json` defaults for forwarded headers, HTTPS redirection, Swagger disablement, migration disablement, demo-seeding disablement, and Azure Blob preference

### Live Runtime Verification

- `scripts/verify-e2e.ps1` was executed against the real MySQL-backed application
- `dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj` was executed against a fresh scratch MySQL database
- `powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "Server=...;Database=...;User=...;Password=...;"` was executed against a second fresh scratch MySQL database
- `powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -ConnectionString "Server=...;Database=...;User=...;Password=...;" -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql` generated an idempotent SQL migration script
- a production-style API run using `MYSQLCONNSTR_DefaultConnection` booted successfully after migrations and seeded reference data, bootstrap admin data, and optional demo data by configuration
- a live API run using local storage uploaded a manager center image successfully, returned an `/uploads/center-images/...` URL, and served that image back publicly
- the Azure Blob provider path was verified through automated endpoint tests against a local fake blob endpoint:
  - manager upload returned a blob-backed public URL
  - the uploaded bytes were publicly reachable from that returned URL
  - invalid uploads were rejected before any blob write was attempted
  - deleting the image removed the backing blob
  - verified flows:
    - registration
    - email verification
    - login
    - forgot-password and reset-password
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
    - center visit tracking and follow/subscription deduplication
    - manager majlis create, update, delete
    - manager announcement creation
    - notification preference persistence
    - in-app and email notifications for majalis and event announcements
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
    - register, resend-verification, verify-email, forgot-password, reset-password, profile notification preferences, and notification dropdown/page flows work in the real frontend
    - the manager gallery UI shows clear errors for invalid file types and oversized images
  - valid manager uploads complete without the old API-reachability failure and refresh the gallery
  - the public center details page renders a hero image and gallery from uploaded media
  - the admin image moderation section can load and delete gallery items successfully
  - `scripts/verify-mobile-frontend.ps1` exercised mobile, tablet, and desktop browser viewports against the real local app and confirmed:
    - home, centers, center details, login, register, profile, dashboard, manager, and admin pages render without horizontal overflow or console errors
    - the hamburger menu appears on small screens, opens, closes, navigates correctly, marks active links, and switches correctly after register, login, and logout
    - touch targets remain usable on key pages and forms stay within the viewport
    - relative routes, relative manifest entry points, and the guarded service-worker registration path remain compatible with future Capacitor packaging

Future developers should rerun:

```powershell
dotnet test NoorLocator.sln
powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=...;Password=...;"
powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=...;Password=...;" -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql
powershell -ExecutionPolicy Bypass -File .\scripts\verify-e2e.ps1 -StartApp -BaseUrl http://127.0.0.1:5210 -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=...;Password=...;"
powershell -ExecutionPolicy Bypass -File .\scripts\verify-mobile-frontend.ps1 -StartApp -BaseUrl http://127.0.0.1:5210 -ConnectionString "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=...;Password=...;"
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
