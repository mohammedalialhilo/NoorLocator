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
- [ ] Do **not** substitute Azure SQL Database or SQL Server for the current NoorLocator deployment; the application and EF migrations are built for MySQL

## App Service Package

- [ ] Confirm the Azure App Service runtime stack is `.NET 10`
- [ ] Run `powershell -ExecutionPolicy Bypass -File .\scripts\package-app-service-api.ps1`
- [ ] Confirm the publish output exists at `.\artifacts\publish\api`
- [ ] Confirm the deployment ZIP exists at `.\artifacts\packages\noorlocator-api-appservice.zip`
- [ ] Confirm the publish output does not contain stale `frontend/uploads` files
- [ ] Confirm the publish output still contains the bundled `frontend/` static site and `frontend/assets/logo_bkg.png`
- [ ] Prefer deploying the published ZIP package instead of the raw repo
- [ ] If deploying source code instead of the published package, set `PROJECT=NoorLocator.Api/NoorLocator.Api.csproj`

## Frontend Strategy

- [ ] Use the existing bundled frontend served by the ASP.NET app
- [ ] Do not create a separate static-site deployment for the current NoorLocator production setup
- [ ] Leave `Frontend__ApiBaseUrl` empty for same-origin hosting, or set it to the public app origin/app root without `/api`
- [ ] Set `Frontend__PublicOrigin` to the public frontend origin used by browsers
- [ ] Confirm `frontend/assets/logo_bkg.png` resolves in the deployed site for the favicon, navbar, hero, workspace, and footer branding

## Custom Domains And HTTPS

- [ ] Decide the launch frontend origin, for example `https://www.noorlocator.example`
- [ ] Prefer the current same-origin deployment shape where the frontend domain and API origin are the same host
- [ ] If you intentionally use a separate API origin such as `https://api.noorlocator.example`, set `Frontend__ApiBaseUrl` to that API origin and keep `Frontend__PublicOrigin` on the browser-facing frontend domain
- [ ] Add the custom host name(s) to Azure App Service
- [ ] Bind a valid TLS certificate to every host name
- [ ] Enable App Service `HTTPS Only`
- [ ] Keep `ReverseProxy__UseForwardedHeaders=true`
- [ ] Keep `Https__RedirectionEnabled=true`
- [ ] Verify `http://` requests redirect to `https://`
- [ ] Lock `Cors__AllowedOrigins__*` to the real production browser origins only
- [ ] If you later enable host filtering, set `AllowedHosts` to the final production host names instead of `*`

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
- [ ] If generating the idempotent script for a production-like environment, pass the production connection string explicitly to `scripts/generate-db-migration-script.ps1`
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
- [ ] Do not keep `MediaStorage__Provider=Local` on a relative path for App Service package deployments; use Azure Blob or an absolute writable path under `HOME`

## App Service Settings

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `WEBSITE_RUN_FROM_PACKAGE=1` when deploying the published package
- [ ] `Jwt__Key`
- [ ] `Jwt__Issuer`
- [ ] `Jwt__Audience`
- [ ] `MySql__ServerVersion`
- [ ] `Frontend__ApiBaseUrl`
- [ ] Keep `Frontend__ApiBaseUrl` empty for same-origin hosting, or set it to the app origin/app root without `/api`
- [ ] `Frontend__PublicOrigin`
- [ ] `Cors__AllowedOrigins__0`
- [ ] `Cors__AllowedOrigins__1`
- [ ] `ReverseProxy__UseForwardedHeaders`
- [ ] `Https__RedirectionEnabled=true`
- [ ] `Swagger__Enabled=false`
- [ ] `Seeding__ApplyMigrations=false`
- [ ] `Seeding__SeedReferenceData=true` only for the first bootstrap that needs reference content
- [ ] `Seeding__SeedAdminAccount=true` only for the first bootstrap that needs an initial admin
- [ ] `Seeding__AdminName`
- [ ] `Seeding__AdminEmail`
- [ ] `Seeding__AdminPassword`
- [ ] Turn `Seeding__SeedAdminAccount=false` again after the admin account is created
- [ ] `Seeding__SeedDemoData=false`
- [ ] Leave the App Service startup command empty for the published NoorLocator API package
- [ ] Set the App Service Health Check path to `/api/health/ping`

## Connection Strings Placement

- [ ] Prefer the App Service Connection strings blade with name `DefaultConnection`
- [ ] If using app settings instead, set `MYSQLCONNSTR_DefaultConnection`
- [ ] `ConnectionStrings__DefaultConnection` is supported as a fallback
- [ ] `AZURE_MYSQL_CONNECTIONSTRING` is supported as a fallback
- [ ] `AzureBlobStorage__ConnectionString` belongs in App Service app settings or a Key Vault reference if you are not using managed identity

## GitHub Actions Deployment Settings

- [ ] Create a GitHub environment named `production`
- [ ] Add GitHub environment secret `AZURE_CLIENT_ID`
- [ ] Add GitHub environment secret `AZURE_TENANT_ID`
- [ ] Add GitHub environment secret `AZURE_SUBSCRIPTION_ID`
- [ ] Add GitHub environment variable `AZURE_WEBAPP_NAME`
- [ ] Configure Azure federated credentials for GitHub OIDC with the subject `repo:<ORG>/<REPO>:environment:production`
- [ ] Confirm the workflow file `.github/workflows/noorlocator-azure-app-service.yml` is the deployment workflow used by `main`

## Secrets

- [ ] Store JWT secret securely
- [ ] Store MySQL credentials securely unless you switch to passwordless DB auth in a later phase
- [ ] Store Blob connection string securely if you are not using managed identity

## Production Hardening

- [ ] Confirm the app does not expose Swagger in production unless intentionally enabled
- [ ] Confirm `/api/health` does not expose the environment name in production
- [ ] Confirm unhandled production failures return the generic NoorLocator error payload instead of detailed exception traces
- [ ] Confirm no development exception page is enabled in the production host
- [ ] Confirm auth uses bearer tokens and not a required auth cookie in the current launch model
- [ ] If any auth cookies are introduced later, require `Secure`, `HttpOnly`, `SameSite`, and CSRF review before launch

## Production Smoke Test Checklist

- [ ] `GET /api/health/ping` returns `200`
- [ ] `GET /api/health` returns `200`
- [ ] `GET /js/runtime-config.js` returns the expected production API base URL
- [ ] `GET /` serves the frontend shell
- [ ] `GET /assets/logo_bkg.png` returns `200`
- [ ] `GET /about` returns the branded About page
- [ ] `GET /register.html` and `GET /login.html` load correctly from the final production origin
- [ ] Registration succeeds for a fresh user
- [ ] Login succeeds for user, manager, and admin accounts
- [ ] Logout clears the session and old protected requests return `401`
- [ ] `dashboard.html` loads for a user account
- [ ] `manager.html` loads for a manager account
- [ ] `admin.html` loads for an admin account
- [ ] `GET /api/languages` returns the expected reference languages if bootstrap seeding was enabled
- [ ] `GET /api/centers` returns `200`
- [ ] User center-request submission succeeds
- [ ] Manager majlis create/update/delete succeeds
- [ ] Manager announcement create/delete succeeds
- [ ] `POST /api/auth/login` succeeds for the bootstrap admin account if first-run admin seeding was enabled
- [ ] `GET /api/auth/me` succeeds with the issued JWT
- [ ] `GET /api/admin/dashboard` succeeds with the bootstrap admin JWT
- [ ] Cross-origin requests from the production frontend origin receive the expected CORS headers
- [ ] Login and logout work through the deployed frontend pages
- [ ] Protected pages such as `dashboard.html`, `manager.html`, and `admin.html` bootstrap correctly after login
- [ ] Uploads succeed with the configured storage provider
- [ ] Manager image uploads return the expected local `/uploads/...` URL or blob/CDN URL for the active provider
- [ ] Uploaded images render correctly in public pages
- [ ] Admin approvals for center requests, manager requests, and language suggestions succeed when launch data depends on them
- [ ] Run `powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-deployed-api.ps1 -BaseUrl https://your-app-name.azurewebsites.net ...`
- [ ] Run `powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-frontend.ps1 -BaseUrl https://your-app-name.azurewebsites.net ...`
- [ ] Run `powershell -ExecutionPolicy Bypass -File .\scripts\verify-mobile-frontend.ps1 -BaseUrl https://your-app-name.azurewebsites.net ...` for an additional responsive/mobile-navbar browser pass

## Rollback And Basic Recovery

- [ ] If deployment fails, confirm the GitHub Actions package artifact came from the intended commit and still contains the published frontend plus `NoorLocator.Api.dll`
- [ ] Check App Service Log Stream and startup logs before changing code
- [ ] Reconfirm `MYSQLCONNSTR_DefaultConnection` or the chosen connection-string override
- [ ] Reconfirm `Jwt__Key` exists and is at least `32` characters
- [ ] Reconfirm `Frontend__PublicOrigin`, `Frontend__ApiBaseUrl`, and every `Cors__AllowedOrigins__*` value match the final launch domains exactly
- [ ] Reconfirm `MediaStorage__Provider=AzureBlob` and the corresponding `AzureBlobStorage__*` settings
- [ ] Reconfirm the Azure Blob container exists, is reachable, and has the intended public-read behavior for NoorLocator image URLs
- [ ] Reconfirm EF Core migrations were applied to the production database before the app started serving traffic
- [ ] Validate database configuration with `/api/health`, `/api/centers`, and `/api/admin/dashboard`
- [ ] Validate storage configuration with a manager image upload followed by a public fetch of the returned image URL
- [ ] If rollback is needed, redeploy the previous known-good package and rerun the production smoke checklist before reopening traffic

## Final Deployment Verification Notes

- [ ] Document the exact production frontend origin and any optional API origin used for launch
- [ ] Document the commit SHA and package artifact used for launch
- [ ] Document whether the launch used same-origin hosting or a separate API subdomain
- [ ] Document the final database connection strategy and migration method used
- [ ] Document the final media storage strategy and validation result
- [ ] Document the date and operator for the final smoke-test pass
