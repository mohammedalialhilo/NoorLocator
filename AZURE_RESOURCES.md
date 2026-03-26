# Azure Resources For NoorLocator

## Required Resources

### Resource Group

- One resource group to hold the production deployment resources for NoorLocator
- Keep App Service, MySQL, and Storage in the same Azure region unless you have a specific cross-region design

### App Service Plan

- One Azure App Service Plan for the NoorLocator API and bundled frontend
- Choose a tier that matches traffic and memory needs
- If you plan to use private networking for MySQL or Storage, use a plan/tier that supports the networking features you need

### Azure App Service

- One Azure App Service running the NoorLocator ASP.NET Core app
- Enable a system-assigned managed identity if you want the app to authenticate to Azure Blob Storage without a storage secret
- Use App Settings / Connection Strings to provide the production configuration values listed in `DEPLOYMENT_CHECKLIST.md`

### Azure Database For MySQL

- Use Azure Database for MySQL Flexible Server
- Create one production server and one `Noorlocator` database
- Decide up front whether the app connects through public access plus firewall rules or private access through virtual networking
- TLS is enabled by default on the service, so the application connection string must be Azure-compatible

### Azure Storage Account

- One Storage Account for uploaded images
- Standard general-purpose v2 is sufficient for the current image-upload workload
- Put it in the same region as the app when possible

### Blob Container

- One blob container, for example `uploads`
- The current app stores direct image URLs, so the delivered image URL must be publicly reachable by browsers
- If you want private blobs later, you will need a follow-up design for signed URLs or an application proxy endpoint

## Required App Service Configuration Keys

### Database

- `MYSQLCONNSTR_DefaultConnection`
- `ConnectionStrings__DefaultConnection`
- `AZURE_MYSQL_CONNECTIONSTRING`

The app now accepts all three patterns. Use one.

### JWT

- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`

### Frontend And CORS

- `Frontend__ApiBaseUrl`
- `Frontend__PublicOrigin`
- `Cors__AllowedOrigins__0`
- `Cors__AllowedOrigins__1`

### Media Storage

- `MediaStorage__Provider`
- `MediaStorage__PublicBasePath`
- `AzureBlobStorage__ConnectionString`
- `AzureBlobStorage__ServiceUri`
- `AzureBlobStorage__AccountName`
- `AzureBlobStorage__ContainerName`
- `AzureBlobStorage__PublicBaseUrl`
- `AzureBlobStorage__ManagedIdentityClientId`
- `AzureBlobStorage__CreateContainerIfMissing`

### Operational Settings

- `ASPNETCORE_ENVIRONMENT=Production`
- `MySql__ServerVersion`
- `Seeding__ApplyMigrations`
- `Seeding__SeedReferenceData`
- `Seeding__SeedDemoData`
- `ReverseProxy__UseForwardedHeaders`
- `Https__RedirectionEnabled`
- `Swagger__Enabled`

## Identity And Secret Options

### Recommended

- Store JWT secrets in App Service settings or Key Vault references
- Use App Service managed identity for Azure Blob access
- Grant the app identity blob data access on the storage account or target container

### Acceptable Temporary Option

- Use `AzureBlobStorage__ConnectionString` until managed identity is ready

## Deployment Shape This Codebase Supports

- App Service hosting the API and static frontend together
- Azure Database for MySQL Flexible Server as the relational database
- Azure Blob Storage as the image provider via `MediaStorage__Provider=AzureBlob`
- Local storage remains available for development and temporary fallback environments
