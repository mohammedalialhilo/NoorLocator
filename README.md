# NoorLocator

NoorLocator is a location-based web platform for discovering Shia Islamic centers, publishing majalis through verified center managers, and protecting data integrity through moderated workflows. This Phase 1 delivery builds the full project scaffold and runnable architecture without implementing the full business feature set yet.

Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.

## Project Overview

- Clean architecture split into API, Application, Domain, Infrastructure, and `frontend/`.
- ASP.NET Core Web API backend with JWT wiring, Swagger, CORS, SQL Server configuration, and EF Core persistence structure.
- Static HTML/CSS/JavaScript frontend served directly by the API from the `frontend/` folder.
- Placeholder endpoints and services that keep the code runnable while reserving business workflows for later phases.

## Architecture Overview

```text
NoorLocator.sln
|-- NoorLocator.Api
|-- NoorLocator.Application
|-- NoorLocator.Domain
|-- NoorLocator.Infrastructure
|-- frontend
|-- NoorLocator
```

- `NoorLocator.Api`: Startup wiring, controllers, Swagger, CORS, JWT authentication setup, and health endpoints.
- `NoorLocator.Application`: DTOs, service interfaces, shared response models, configuration contracts, and validation structure.
- `NoorLocator.Domain`: Core entities and enums derived from the supplied specification.
- `NoorLocator.Infrastructure`: EF Core `DbContext`, entity configurations, language seed data, and placeholder service implementations.
- `frontend`: Branded multi-page scaffold with a shared design system, reusable layout, and API-connected placeholder pages.
- `NoorLocator`: Existing manifesto content left untouched in the workspace.

## Setup Instructions

1. Ensure SQL Server or LocalDB is available.
2. Update the connection string and JWT values in [appsettings.json](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Api/appsettings.json) or [appsettings.Development.json](/c:/Users/alhil/Desktop/NoorLocator/NoorLocator/NoorLocator.Api/appsettings.Development.json).
3. Restore and build the solution:

   ```powershell
   dotnet restore
   dotnet build NoorLocator.sln
   ```

4. Run the API:

   ```powershell
   dotnet run --project .\NoorLocator.Api
   ```

5. Open the app:
   - Frontend landing page: `https://localhost:<port>/`
   - Swagger UI: `https://localhost:<port>/swagger`
   - Health endpoint: `https://localhost:<port>/api/health`

## Phase 1 Notes

- Authentication endpoints are scaffolded but intentionally return placeholder responses.
- Domain entities cover the major models from the PDF specification.
- `Languages` are seeded structurally with Arabic, Swedish, English, and Farsi.
- Frontend pages are scaffolded and connected to the placeholder API responses where appropriate.
- The PDF specification remains the source of truth for future phases.
