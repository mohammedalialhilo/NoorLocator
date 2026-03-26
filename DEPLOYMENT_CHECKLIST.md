# NoorLocator Deployment Checklist

## Azure Resources

- [ ] Create the production Resource Group
- [ ] Create the App Service Plan
- [ ] Create the Azure App Service for NoorLocator
- [ ] Enable a system-assigned managed identity on the App Service if Blob access will be secretless
- [ ] Create the Azure Database for MySQL Flexible Server
- [ ] Create the `Noorlocator` MySQL database
- [ ] Create the Azure Storage Account
- [ ] Create the blob container for uploads, such as `uploads`

## Database Setup

- [ ] Choose public-access firewall rules or private networking for MySQL
- [ ] Confirm the final MySQL host name, database name, username, and TLS-ready connection string
- [ ] If the host ends with `.mysql.database.azure.com`, confirm the final connection string uses TLS or let NoorLocator add `SslMode=Required`
- [ ] Prefer `MYSQLCONNSTR_DefaultConnection` on Azure App Service, or use `ConnectionStrings__DefaultConnection` / `AZURE_MYSQL_CONNECTIONSTRING`
- [ ] Set one of:
- [ ] `MYSQLCONNSTR_DefaultConnection`
- [ ] `ConnectionStrings__DefaultConnection`
- [ ] `AZURE_MYSQL_CONNECTIONSTRING`
- [ ] Choose the migration path:
- [ ] `dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj`
- [ ] or `powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "..."`
- [ ] Optionally generate and archive `.\artifacts\noorlocator-mysql-idempotent.sql` with `scripts/generate-db-migration-script.ps1`
- [ ] Run EF Core migrations against the Azure MySQL database before the production app instance serves traffic
- [ ] Decide whether first-run bootstrap should seed reference data and an initial admin account
- [ ] Keep `Seeding__SeedDemoData=false` in production

## Storage Setup

- [ ] Set `MediaStorage__Provider=AzureBlob`
- [ ] Set `AzureBlobStorage__ContainerName`
- [ ] Choose one blob auth path:
- [ ] `AzureBlobStorage__ConnectionString`
- [ ] or `AzureBlobStorage__ServiceUri` plus App Service managed identity
- [ ] If using a user-assigned identity, set `AzureBlobStorage__ManagedIdentityClientId`
- [ ] Keep the NoorLocator image container publicly readable at the blob level because the app stores direct browser-facing image URLs
- [ ] If the app should create the container, set `AzureBlobStorage__CreateContainerIfMissing=true`
- [ ] If the app creates the container, keep `AzureBlobStorage__UseBlobPublicAccess=true` so new containers are created with blob-level anonymous read
- [ ] If image URLs should use a custom public endpoint, set `AzureBlobStorage__PublicBaseUrl` to the public container root or CDN origin for that container
- [ ] Decide whether the app may auto-create the container with `AzureBlobStorage__CreateContainerIfMissing`
- [ ] Ensure uploaded image URLs are browser-reachable in the final production design

## App Service Settings

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `Jwt__Key`
- [ ] `Jwt__Issuer`
- [ ] `Jwt__Audience`
- [ ] `MySql__ServerVersion`
- [ ] `Frontend__ApiBaseUrl`
- [ ] `Frontend__PublicOrigin`
- [ ] `Cors__AllowedOrigins__0`
- [ ] `Cors__AllowedOrigins__1`
- [ ] `ReverseProxy__UseForwardedHeaders`
- [ ] `Https__RedirectionEnabled`
- [ ] `Swagger__Enabled=false`
- [ ] `Seeding__ApplyMigrations=false`
- [ ] `Seeding__SeedReferenceData=true` only for the first bootstrap that needs reference content
- [ ] `Seeding__SeedAdminAccount=true` only for the first bootstrap that needs an initial admin
- [ ] `Seeding__AdminName`
- [ ] `Seeding__AdminEmail`
- [ ] `Seeding__AdminPassword`
- [ ] Turn `Seeding__SeedAdminAccount=false` again after the admin account is created
- [ ] `Seeding__SeedDemoData=false`

## Secrets

- [ ] Store JWT secret securely
- [ ] Store MySQL credentials securely unless you switch to passwordless DB auth in a later phase
- [ ] Store Blob connection string securely if you are not using managed identity

## Final Verification

- [ ] `GET /api/health/ping` returns `200`
- [ ] `GET /api/health` returns `200`
- [ ] `GET /js/runtime-config.js` returns the expected production API base URL
- [ ] `GET /` serves the frontend shell
- [ ] `GET /api/languages` returns the expected reference languages if bootstrap seeding was enabled
- [ ] `GET /api/centers` returns `200`
- [ ] `POST /api/auth/login` succeeds for the bootstrap admin account if first-run admin seeding was enabled
- [ ] Cross-origin requests from the production frontend origin receive the expected CORS headers
- [ ] Uploads succeed with the configured storage provider
- [ ] Manager image uploads return the expected local `/uploads/...` URL or blob/CDN URL for the active provider
- [ ] Uploaded images render correctly in public pages
