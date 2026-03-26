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
- [ ] Set one of:
- [ ] `MYSQLCONNSTR_DefaultConnection`
- [ ] `ConnectionStrings__DefaultConnection`
- [ ] `AZURE_MYSQL_CONNECTIONSTRING`
- [ ] Run EF Core migrations against the Azure MySQL database
- [ ] Keep `Seeding__SeedDemoData=false` in production

## Storage Setup

- [ ] Set `MediaStorage__Provider=AzureBlob`
- [ ] Set `AzureBlobStorage__ContainerName`
- [ ] Choose one blob auth path:
- [ ] `AzureBlobStorage__ConnectionString`
- [ ] or `AzureBlobStorage__ServiceUri` plus App Service managed identity
- [ ] If using a user-assigned identity, set `AzureBlobStorage__ManagedIdentityClientId`
- [ ] If image URLs should use a custom public endpoint, set `AzureBlobStorage__PublicBaseUrl`
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
- [ ] `Seeding__ApplyMigrations=true`
- [ ] `Seeding__SeedReferenceData=false` unless you intentionally need seed content

## Secrets

- [ ] Store JWT secret securely
- [ ] Store MySQL credentials securely unless you switch to passwordless DB auth in a later phase
- [ ] Store Blob connection string securely if you are not using managed identity

## Final Verification

- [ ] `GET /api/health/ping` returns `200`
- [ ] `GET /api/health` returns `200`
- [ ] `GET /js/runtime-config.js` returns the expected production API base URL
- [ ] `GET /` serves the frontend shell
- [ ] `GET /api/centers` returns `200`
- [ ] Cross-origin requests from the production frontend origin receive the expected CORS headers
- [ ] Uploads succeed with the configured storage provider
- [ ] Uploaded images render correctly in public pages
