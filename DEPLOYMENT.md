# NoorLocator Deployment

## Production Prerequisites

- MySQL 8.x reachable from the API host
- .NET 10 runtime if you are not using the container image
- A real JWT signing key with at least 32 characters
- At least one allowed frontend origin when the frontend is hosted on a different origin
- A writable upload directory or mounted volume for `MediaStorage:RelativeRootPath`

## Configuration Layout

- Base defaults: `NoorLocator.Api/appsettings.json`
- Local development overrides: `NoorLocator.Api/appsettings.Development.json`
- Production-safe overrides: `NoorLocator.Api/appsettings.Production.json`
- Environment variable overrides:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Jwt__Issuer
Jwt__Audience
Cors__AllowedOrigins__0
Cors__AllowedOrigins__1
Frontend__RelativeRootPath
MediaStorage__RelativeRootPath
MediaStorage__PublicBasePath
Seeding__ApplyMigrations
Seeding__SeedReferenceData
Seeding__SeedDemoData
ReverseProxy__UseForwardedHeaders
Https__RedirectionEnabled
Swagger__Enabled
```

## Production Defaults

- Demo users and demo centers are disabled by default in production
- Swagger is disabled by default in production
- HTTPS redirection is disabled by default in production so reverse-proxy deployments can terminate TLS upstream
- Forwarded headers are enabled by default in production appsettings
- Uploads are served from the configured `/uploads` path instead of depending on the frontend folder

## Frontend API Base URL

- When NoorLocator serves the frontend itself, the frontend uses same-origin by default
- In the current production design, keep the frontend bundled with the ASP.NET app instead of deploying a separate static site
- If you set `Frontend__ApiBaseUrl`, use the app origin/app root without `/api`
- When the frontend is hosted separately in a future phase, set `window.NoorLocatorRuntimeConfig.apiBaseUrl` to the API origin/app root without `/api`
- The frontend no longer contains hardcoded localhost API URLs

## Container Setup

1. Copy `.env.example` to `.env`
2. Replace every placeholder value
3. Start the stack with `docker compose up --build`

The compose file now expects secrets and origins from `.env` and mounts uploads to a dedicated volume.

## Recommended Verification

After setting production environment variables and starting the app:

1. `GET /api/health/ping` should return `200`
2. `GET /api/health` should return `200` and should not expose the environment name
3. `GET /api/content/about` should return `200` when reference data seeding is enabled
4. `GET /api/centers` should return `200`
5. `GET /` should return the frontend shell
6. `GET /swagger` should return `404` unless `Swagger__Enabled=true`
