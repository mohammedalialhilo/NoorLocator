# NoorLocator Deployment Guide

## SECTION 1 - INTRODUCTION

NoorLocator is a web application for discovering Shia centers, browsing majalis, submitting community information, and managing approved center content.

This guide explains how to deploy NoorLocator in Azure in a way that is suitable for a beginner. It is written to help someone who may not have deployed an ASP.NET application before.

This guide covers the full NoorLocator deployment:

- frontend pages
- backend API
- MySQL database
- image storage in Azure Blob Storage
- optional GitHub Actions deployment automation

Important warning:

- NoorLocator currently uses MySQL, not Azure SQL Database.
- In Azure, the correct database service for NoorLocator is Azure Database for MySQL Flexible Server.
- Do not replace that with Azure SQL Database or SQL Server unless you also change the codebase and EF Core provider.

## SECTION 2 - WHAT YOU NEED BEFORE STARTING

Before you start, make sure you have the following:

- an Azure account with permission to create resources
- a GitHub account
- the NoorLocator source code in a GitHub repository or on your local machine
- access to the Azure subscription where NoorLocator will be deployed
- access to DNS settings for your domain if you plan to use a custom domain

Local tools you should install before starting:

- Git
- .NET SDK 10
- PowerShell
- a code editor such as Visual Studio Code
- MySQL Workbench or another MySQL client so you can inspect the database if needed

Access you may need before deployment:

- permission to create a Resource Group
- permission to create an App Service Plan and Web App
- permission to create Azure Database for MySQL Flexible Server
- permission to create a Storage Account
- permission to create a Microsoft Entra application if you want GitHub Actions with OpenID Connect
- permission to assign Azure roles to that deployment identity

## SECTION 3 - DEPLOYMENT OVERVIEW

Here is the NoorLocator production setup in simple terms:

- one Azure App Service hosts the NoorLocator website and the NoorLocator API together
- the frontend is not deployed as a separate site in the current design
- the database lives in Azure Database for MySQL Flexible Server
- uploaded images live in Azure Blob Storage
- GitHub Actions can build, test, package, and deploy the application automatically

How the parts work together:

- the user opens the NoorLocator site in a browser
- the browser loads the frontend files from Azure App Service
- the frontend calls the NoorLocator API
- the API reads and writes data in Azure Database for MySQL
- the API stores image files in Azure Blob Storage
- the browser displays public image URLs returned by the API

The recommended production shape is same-origin hosting:

- example frontend and API host: `https://www.noorlocator.example`
- the same deployed app serves both the web pages and `/api/*`
- in this model, leave `Frontend__ApiBaseUrl` empty

## SECTION 4 - STEP-BY-STEP AZURE SETUP

### Step 1: Create the Resource Group

1. Sign in to the Azure Portal.
2. In the top search bar, search for `Resource groups`.
3. Click `Resource groups`.
4. Click `Create`.
5. Choose your Azure subscription.
6. Enter a name such as `rg-noorlocator-prod`.
7. Choose the Azure region you want to use.
8. Click `Review + create`.
9. Click `Create`.

Why this is needed:

- a Resource Group keeps all NoorLocator Azure resources together so they are easier to manage

Common mistake:

- creating resources in different Resource Groups by accident makes deployment and billing harder to manage

### Step 2: Create the App Service Plan

1. Search for `App Service Plans`.
2. Click `Create`.
3. Choose the same subscription and Resource Group.
4. Enter a name such as `asp-noorlocator-prod`.
5. Choose the same region as the Resource Group when possible.
6. Choose Linux or Windows according to your team standard. NoorLocator can run on App Service with `.NET 10`.
7. Choose a pricing tier that fits your budget and expected traffic.
8. Click `Review + create`.
9. Click `Create`.

Why this is needed:

- the App Service Plan provides the compute resources used by the web app

### Step 3: Create the App Service

1. Search for `App Services`.
2. Click `Create`.
3. Choose `Web App`.
4. Choose the same subscription and Resource Group.
5. Enter the app name. This becomes part of the Azure default URL, for example `noorlocator-prod-app`.
6. Publish type should be `Code`.
7. Runtime stack should be `.NET 10`.
8. Operating system should match the App Service Plan you created.
9. Select the App Service Plan from Step 2.
10. Click `Review + create`.
11. Click `Create`.

After the App Service is created:

1. Open the Web App.
2. In the left menu, open `Settings` then `Configuration` or `Environment variables`, depending on the Azure Portal layout.
3. In the left menu, open `Health check`.
4. Set the Health Check path to `/api/health/ping`.
5. In the left menu, open `TLS/SSL settings`.
6. Make sure `HTTPS Only` is enabled.

Why this is needed:

- the Web App is the production host for NoorLocator
- the Health Check helps Azure know whether the app is responding
- HTTPS protects login tokens and user data

### Step 4: Create Azure Database for MySQL Flexible Server

Important:

- even if you expected Azure SQL Database, stop and use Azure Database for MySQL Flexible Server for NoorLocator

1. Search for `Azure Database for MySQL flexible servers`.
2. Click `Create`.
3. Choose the same subscription and Resource Group.
4. Enter a server name such as `mysql-noorlocator-prod`.
5. Choose the same region as the app when possible.
6. Choose the MySQL version used by NoorLocator. The current repository expects MySQL `8.0.36`.
7. Enter an administrator username.
8. Enter and confirm a strong administrator password.
9. Choose networking settings.
10. If you are just getting started, public access plus firewall rules is the easiest option.
11. Click `Review + create`.
12. Click `Create`.

After the server is created:

1. Open the MySQL Flexible Server.
2. If public access is enabled, configure firewall rules so your local machine can connect during setup.
3. Create the NoorLocator database named `Noorlocator` using MySQL Workbench or another MySQL client.

Example SQL:

```sql
CREATE DATABASE Noorlocator;
```

Why this is needed:

- NoorLocator stores users, centers, majalis, moderation data, and audit logs in MySQL

Common mistakes:

- using Azure SQL instead of Azure Database for MySQL
- forgetting firewall rules for your local machine
- using the wrong database name in the connection string

### Step 5: Create the Storage Account

1. Search for `Storage accounts`.
2. Click `Create`.
3. Choose the same subscription and Resource Group.
4. Enter a name such as `stnoorlocatorprod`.
5. Choose the same region as the app when possible.
6. Keep the default performance and redundancy unless your organization has a different requirement.
7. Click `Review + create`.
8. Click `Create`.

Why this is needed:

- NoorLocator stores uploaded images in Azure Blob Storage in production

### Step 6: Create the Blob Container

1. Open the Storage Account.
2. In the left menu, click `Containers`.
3. Click `+ Container`.
4. Enter the container name `uploads`.
5. Set anonymous access according to the NoorLocator launch model:
   - NoorLocator stores direct browser-facing image URLs
   - the container should allow blob-level public read access if the app is returning public image URLs directly
6. Click `Create`.

Also check the Storage Account anonymous access setting:

1. In the Storage Account, open `Configuration`.
2. Make sure blob anonymous access is allowed if you want public browser image URLs.

Why this is needed:

- NoorLocator public center pages display images directly from storage URLs

Common mistake:

- creating the container but keeping blob anonymous access disabled when the app expects public image URLs

## SECTION 5 - STEP-BY-STEP BACKEND DEPLOYMENT

### Step 1: Understand what gets deployed

In the current NoorLocator architecture:

- the backend API and frontend files are deployed together
- `NoorLocator.Api/NoorLocator.Api.csproj` copies the `frontend/` folder into the published output
- the App Service package contains both the API DLLs and the frontend files

### Step 2: Put the correct settings into App Service

Open the NoorLocator App Service and go to `Settings` then `Configuration` or `Environment variables`.

Add these important settings:

- `ASPNETCORE_ENVIRONMENT=Production`
- `WEBSITE_RUN_FROM_PACKAGE=1`
- `Jwt__Key=<a strong secret at least 32 characters long>`
- `Jwt__Issuer=NoorLocator`
- `Jwt__Audience=NoorLocator.Client`
- `MySql__ServerVersion=8.0.36`
- `ReverseProxy__UseForwardedHeaders=true`
- `Https__RedirectionEnabled=true`
- `Swagger__Enabled=false`
- `Seeding__ApplyMigrations=false`
- `Seeding__SeedDemoData=false`

Frontend settings:

- `Frontend__ApiBaseUrl=` leave this empty for same-origin hosting
- `Frontend__PublicOrigin=https://www.noorlocator.example` or your final browser origin
- `Cors__AllowedOrigins__0=https://www.noorlocator.example`

First bootstrap only:

- `Seeding__SeedReferenceData=true`
- `Seeding__SeedAdminAccount=true`
- `Seeding__AdminName=NoorLocator Admin`
- `Seeding__AdminEmail=admin@your-domain.example`
- `Seeding__AdminPassword=<strong bootstrap admin password>`

After the first successful bootstrap:

- change `Seeding__SeedAdminAccount=false`
- leave `Seeding__SeedReferenceData` on only if your deployment policy allows it

Why these settings matter:

- JWT settings control authentication
- frontend settings control browser-to-API behavior
- CORS settings control which browser origins can call the API
- seeding settings control one-time bootstrap behavior

### Step 3: Put the database connection string in the right place

Preferred option:

1. In App Service, open the `Connection strings` section.
2. Add a new connection string named `DefaultConnection`.
3. Paste the MySQL connection string for the `Noorlocator` database.

Supported alternatives:

- `MYSQLCONNSTR_DefaultConnection`
- `ConnectionStrings__DefaultConnection`
- `AZURE_MYSQL_CONNECTIONSTRING`

Recommended connection string pattern:

```text
Server=your-server.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=YOUR_USER;Password=YOUR_PASSWORD;
```

Why this is needed:

- without the correct connection string, NoorLocator cannot start or reach the database

### Step 4: Put Blob Storage settings in App Service

Set these:

- `MediaStorage__Provider=AzureBlob`
- `AzureBlobStorage__ContainerName=uploads`

Choose one authentication path:

Option A: Connection string

- `AzureBlobStorage__ConnectionString=<your storage connection string>`

Option B: Managed identity

- enable system-assigned managed identity on the App Service
- grant Blob permissions in Azure
- set `AzureBlobStorage__ServiceUri=https://yourstorageaccount.blob.core.windows.net`
- if you use a user-assigned identity, also set `AzureBlobStorage__ManagedIdentityClientId`

Optional:

- `AzureBlobStorage__PublicBaseUrl=<your blob public base URL or CDN URL>`
- `AzureBlobStorage__CreateContainerIfMissing=true` if you want the app to create the container
- `AzureBlobStorage__UseBlobPublicAccess=true` if the app may create a blob-public container

### Step 5: Publish and deploy the backend package

Recommended local packaging commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-app-service-api.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\verify-app-service-package.ps1
```

Expected output:

- publish folder: `artifacts/publish/api`
- deployment ZIP: `artifacts/packages/noorlocator-api-appservice.zip`

If you deploy manually in Azure:

1. Open the App Service.
2. Open the deployment area you prefer, such as Deployment Center or ZIP deploy tooling used by your team.
3. Upload the ZIP package created above, or use the GitHub Actions workflow in Section 10.

### Step 6: Verify the backend is working

After deployment, verify:

- `https://your-host/api/health/ping` returns `pong`
- `https://your-host/api/health` returns `200`
- `https://your-host/js/runtime-config.js` loads
- `https://your-host/` loads the NoorLocator home page

## SECTION 6 - STEP-BY-STEP DATABASE SETUP

### Step 1: Connect to Azure Database for MySQL

The easiest beginner-friendly approach is MySQL Workbench.

1. Open MySQL Workbench.
2. Create a new connection.
3. Enter the MySQL server host name from Azure.
4. Enter port `3306`.
5. Enter the admin username you created in Azure.
6. Enter the password.
7. Test the connection.

If the connection fails:

- check Azure firewall rules
- check the username and password
- check that you used the MySQL server, not the App Service host name

### Step 2: Apply migrations

Recommended production command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\apply-db-migrations.ps1 -EnvironmentName Production -ConnectionString "Server=your-server.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=YOUR_USER;Password=YOUR_PASSWORD;"
```

Alternative command:

```powershell
dotnet ef database update --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj
```

Optional idempotent script generation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-db-migration-script.ps1 -EnvironmentName Production -ConnectionString "Server=your-server.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=YOUR_USER;Password=YOUR_PASSWORD;" -OutputPath .\artifacts\noorlocator-mysql-idempotent.sql
```

Why this is needed:

- the application tables are created by EF Core migrations

### Step 3: Verify the tables exist

In MySQL Workbench, connect to the `Noorlocator` database and run:

```sql
SHOW TABLES;
```

You should see NoorLocator tables such as:

- `Users`
- `Centers`
- `Majalis`
- `CenterRequests`
- `AuditLogs`

### Step 4: Verify seed data works

For first bootstrap, NoorLocator can seed:

- reference data such as languages
- a bootstrap admin account

After the app starts with first-bootstrap settings enabled:

1. Open `https://your-host/api/languages`
2. Confirm that language data is returned
3. Try logging in with the bootstrap admin account

Important production warning:

- keep `Seeding__SeedDemoData=false` in production
- turn `Seeding__SeedAdminAccount=false` again after the admin account exists

## SECTION 7 - STEP-BY-STEP IMAGE STORAGE SETUP

### Step 1: Decide how Blob Storage will authenticate

You have two main options:

- use a Blob connection string
- use App Service managed identity

The connection-string approach is easier for beginners.

### Step 2: Add Blob Storage settings

Set these in App Service:

- `MediaStorage__Provider=AzureBlob`
- `AzureBlobStorage__ContainerName=uploads`

If you use a connection string:

- `AzureBlobStorage__ConnectionString=<storage connection string>`

If you use managed identity:

- `AzureBlobStorage__ServiceUri=https://yourstorageaccount.blob.core.windows.net`
- enable managed identity on the App Service
- grant Blob permissions in Azure to that identity

### Step 3: Understand how NoorLocator uses Blob Storage

The flow is:

- a manager uploads an image
- the NoorLocator API validates the file
- the API uploads it to Blob Storage
- the API stores the returned public URL
- the frontend displays that URL on public center pages

### Step 4: Verify uploads work

After deployment:

1. Log in with a manager account.
2. Open the manager workspace.
3. Upload a valid image.
4. Confirm the API returns success.
5. Open the returned image URL in a browser tab.
6. Open the public center details page and confirm the uploaded image is visible.

Common mistakes:

- `MediaStorage__Provider` left as `Local`
- container name does not match the real Azure container
- connection string or Service URI is wrong
- blob public access is disabled when NoorLocator expects public image URLs

## SECTION 8 - STEP-BY-STEP FRONTEND DEPLOYMENT

### Important architecture note

NoorLocator does not currently deploy the frontend as a separate static site in the recommended production setup.

Instead:

- the frontend files are bundled into the ASP.NET publish output
- the App Service deployment package includes both frontend and backend

### Step 1: Configure the production API URL

For the recommended same-origin deployment:

- leave `Frontend__ApiBaseUrl` empty

Why:

- the browser will use the same host for the site and the API
- this avoids unnecessary cross-origin complexity

Only set `Frontend__ApiBaseUrl` if:

- you intentionally use a separate API host such as `https://api.noorlocator.example`

If you set it:

- enter the root origin only
- do not add `/api`

Example:

- correct: `https://api.noorlocator.example`
- wrong: `https://api.noorlocator.example/api`

### Step 2: Deploy the frontend

Because the frontend is bundled into the same package, deploying the App Service ZIP deploys both:

- frontend
- backend

There is no extra frontend deployment step in the recommended NoorLocator design.

### Step 3: Verify frontend talks to backend

After deployment:

1. Open the NoorLocator home page.
2. Confirm the center list loads.
3. Open browser developer tools if needed and check that requests go to your production host, not `localhost`.
4. Open `https://your-host/js/runtime-config.js`.
5. Confirm it does not point to an old local development API URL.

Common mistake:

- leaving `Frontend__ApiBaseUrl` pointing to `localhost`

## SECTION 9 - DOMAIN AND HTTPS

### Step 1: Connect the custom domain

1. Open the App Service in Azure Portal.
2. Click `Custom domains`.
3. Click `Add custom domain`.
4. Enter your domain, such as `www.noorlocator.example`.
5. Follow the Azure instructions for the DNS record that must be created.

Typical DNS pattern:

- `www` often uses a CNAME record to the Azure App Service host name
- the root domain may require an A record or ALIAS/ANAME depending on your DNS provider

### Step 2: Enable HTTPS

1. In the App Service, open `TLS/SSL settings`.
2. Bind a valid certificate.
3. Use an App Service Managed Certificate if it fits your domain scenario, or upload/import your own certificate.
4. Make sure `HTTPS Only` is turned on.

### Step 3: Verify the final domain

Check all of the following:

- `http://your-domain` redirects to `https://your-domain`
- the certificate is valid
- login still works
- logout still works
- image upload still works
- public image URLs open correctly

## SECTION 10 - GITHUB ACTIONS / CI-CD

### Step 1: Understand the workflow

The NoorLocator deployment workflow file is:

- `.github/workflows/noorlocator-azure-app-service.yml`

It performs:

- restore
- build
- test
- publish
- deploy to Azure App Service

Workflow behavior:

- pull requests to `main` validate the build but do not deploy
- pushes to `main` deploy to production
- `workflow_dispatch` allows a manual run

### Step 2: Create the GitHub environment

1. Open your GitHub repository.
2. Click `Settings`.
3. Click `Environments`.
4. Click `New environment`.
5. Name it `production`.

### Step 3: Add GitHub environment secrets and variable

In the `production` environment, add these secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Add this variable:

- `AZURE_WEBAPP_NAME`

### Step 4: Configure Azure OpenID Connect

Beginner-friendly Azure steps:

1. In Azure Portal, search for `App registrations`.
2. Click `New registration`.
3. Give it a name such as `NoorLocator GitHub Deploy`.
4. After it is created, copy the `Application (client) ID`.
5. Also note the Azure `Tenant ID`.
6. Open your Azure subscription and copy the `Subscription ID`.

Now grant the app permission to deploy:

1. Open the NoorLocator App Service.
2. Open `Access control (IAM)`.
3. Click `Add role assignment`.
4. Assign the `Website Contributor` role to the GitHub deployment identity.

Now add the federated credential:

1. Go back to the App registration.
2. Open `Certificates & secrets`.
3. Open `Federated credentials`.
4. Click `Add credential`.
5. Choose the GitHub Actions scenario.
6. Enter your GitHub organization.
7. Enter your repository name.
8. Choose the environment `production`.
9. Save it.

The subject expected by NoorLocator is:

```text
repo:<ORG>/<REPO>:environment:production
```

### Step 5: Verify workflow success

After pushing to `main`:

1. Open the `Actions` tab in GitHub.
2. Open the latest workflow run.
3. Confirm the `Build and test` job passed.
4. Confirm the `Publish App Service package` job passed.
5. Confirm the `Deploy to Azure App Service` job passed.
6. Check the workflow summary for the smoke-test results.

## SECTION 11 - FULL TESTING AFTER DEPLOYMENT

After deployment, test NoorLocator in this order.

### Public checks

1. Open the home page.
2. Confirm the NoorLocator branding loads.
3. Open the About page.
4. Open the centers directory.
5. Open a center details page.

### Register

1. Open `register.html`.
2. Create a fresh account.
3. Confirm registration succeeds.
4. Confirm you are redirected into the authenticated experience.

### Login

1. Log out if you are already signed in.
2. Open `login.html`.
3. Sign in with:
   - a normal user
   - a manager
   - an admin
4. Confirm each role lands in the correct workspace.

### Logout

1. Click logout from the navbar or page action.
2. Confirm you return to the logged-out state.
3. Refresh the page.
4. Confirm protected pages no longer load without logging in again.

### Browse centers

1. Open the centers page.
2. Search and filter.
3. Confirm results load.

### Nearest centers

1. Use the nearest center feature if location is available.
2. Confirm results are returned.

### Center details

1. Open a center detail page.
2. Confirm center information, languages, announcements, and images load correctly.

### Center request submission

1. Log in as a normal user.
2. Open the dashboard.
3. Submit a center request.
4. Confirm the request appears in the user request list.

### Suggestion submission

1. Log in as a normal user.
2. Submit a suggestion.
3. Confirm the submission succeeds.

### Manager functions

1. Log in as a manager.
2. Create a majlis.
3. Edit the majlis.
4. Delete the majlis.
5. Create an announcement.
6. Upload an image.
7. Confirm the image upload succeeds.

### Admin approvals

1. Log in as an admin.
2. Open the admin dashboard.
3. Approve a center request.
4. Approve or reject a manager request.
5. Approve or reject a language suggestion.

### Public image display

1. After the manager upload, open the public center details page.
2. Confirm the uploaded image is visible.
3. Open the image URL directly in a browser tab.
4. Confirm the image loads.

### Recommended scripted checks

Run these after deployment when possible:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-deployed-api.ps1 -BaseUrl https://your-host -Origin https://your-host -AdminEmail admin@your-domain.example -AdminPassword YOUR_ADMIN_PASSWORD -ManagerEmail manager@your-domain.example -ManagerPassword YOUR_MANAGER_PASSWORD
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-frontend.ps1 -BaseUrl https://your-host -UserEmail user@your-domain.example -UserPassword YOUR_USER_PASSWORD -ManagerEmail manager@your-domain.example -ManagerPassword YOUR_MANAGER_PASSWORD -AdminEmail admin@your-domain.example -AdminPassword YOUR_ADMIN_PASSWORD
powershell -ExecutionPolicy Bypass -File .\scripts\verify-mobile-frontend.ps1 -BaseUrl https://your-host
```

## SECTION 12 - COMMON ERRORS AND HOW TO FIX THEM

### Error: API not reachable

Check:

- the App Service is running
- the deployment package was uploaded successfully
- `ASPNETCORE_ENVIRONMENT=Production`
- the health endpoint `/api/health/ping`

### Error: CORS issue

Check:

- `Frontend__PublicOrigin`
- every `Cors__AllowedOrigins__*` value
- whether `Frontend__ApiBaseUrl` was set incorrectly

Common mistake:

- leaving an old domain or `localhost` in the production frontend settings

### Error: Database connection failure

Check:

- App Service connection string name is correct
- MySQL firewall rules allow the connection
- the database name is `Noorlocator`
- the connection string points to MySQL, not Azure SQL

### Error: Migration failure

Check:

- the connection string is correct
- the MySQL user has permission to create and update tables
- you passed the production connection string when generating the idempotent script

### Error: Image upload failure

Check:

- `MediaStorage__Provider=AzureBlob`
- Blob connection string or Service URI is correct
- the container name is correct
- the container exists
- the image URL is publicly reachable if the site expects public image URLs

### Error: Logout not working

Check:

- `/api/auth/logout` is reachable
- old JWTs return `401` after logout
- browser storage is cleared
- cached protected pages are not being restored from stale browser state

### Error: Frontend still pointing to localhost

Check:

- `Frontend__ApiBaseUrl`
- `js/runtime-config.js`
- browser cache or an old service worker

## SECTION 13 - FINAL CHECKLIST

Use this simple final checklist before launch:

- Resource Group created
- App Service Plan created
- App Service created
- Azure Database for MySQL Flexible Server created
- `Noorlocator` database created
- Storage Account created
- `uploads` blob container created
- production app settings entered
- production connection string entered
- JWT settings entered
- Blob settings entered
- migrations applied
- first bootstrap admin created
- `Seeding__SeedAdminAccount` turned off after bootstrap
- `Seeding__SeedDemoData=false`
- frontend loads from the production host
- login works
- logout works
- manager image upload works
- public image display works
- admin approvals work
- custom domain works if used
- HTTPS is enabled
- GitHub Actions workflow passes if CI/CD is enabled

End of guide.
