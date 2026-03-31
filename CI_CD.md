# NoorLocator CI/CD

## Overview

NoorLocator now ships with a GitHub Actions workflow at `.github/workflows/noorlocator-azure-app-service.yml` for:

- restore
- build
- test
- publish
- deploy to Azure App Service

The frontend is **not** deployed separately right now. `NoorLocator.Api/NoorLocator.Api.csproj` copies `../frontend/**/*` into the ASP.NET publish output, so the GitHub Actions workflow deploys one App Service package that contains both the API and the static frontend.

## Deployment model

- Pull requests to `main` run build, test, and publish package validation, but do not deploy.
- Pushes to `main` run build, test, publish, and deploy to the GitHub environment named `production`.
- `workflow_dispatch` can be used to redeploy manually from `main`.
- The deploy job uses GitHub Environment support so you can add required reviewers, branch restrictions, and environment-scoped secrets.

## What the workflow verifies

Before deployment, the workflow verifies that:

- `dotnet restore`, `dotnet build`, and `dotnet test` all succeed
- TRX test files are produced and uploaded as workflow artifacts
- the publish output exists at `artifacts/publish/api`
- the deployment ZIP exists at `artifacts/packages/noorlocator-api-appservice.zip`
- the publish output contains:
  - `NoorLocator.Api.dll`
  - `frontend/index.html`
  - `frontend/assets/logo_bkg.png`
- the publish output and ZIP do not contain unexpected `frontend/uploads` content

After deployment, the workflow smoke-tests:

- `/api/health/ping`
- `/js/runtime-config.js`
- `/`

## Required GitHub environment configuration

Create a GitHub environment named `production` and add the following values there.

### Secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

### Variables

- `AZURE_WEBAPP_NAME`

The workflow is intentionally written for Azure OpenID Connect authentication instead of a long-lived publish profile secret.

## Azure federated authentication setup

Azure App Service documentation recommends OpenID Connect for GitHub Actions because it uses short-lived tokens instead of stored deployment credentials.

Set up Azure so the `production` GitHub environment can request tokens:

1. Create or reuse a Microsoft Entra application and service principal for GitHub Actions.
2. Assign the identity the `Website Contributor` role scoped to the NoorLocator App Service.
3. Add a federated credential with:
   - issuer: `https://token.actions.githubusercontent.com`
   - audience: `api://AzureADTokenExchange`
   - subject: `repo:<ORG>/<REPO>:environment:production`
4. Copy the application/client ID, tenant ID, and subscription ID into the GitHub `production` environment secrets listed above.
5. Set `AZURE_WEBAPP_NAME` to the App Service name in the same GitHub `production` environment.

If you decide not to use a GitHub environment for deployment, the subject must instead target the branch ref, for example:

```text
repo:<ORG>/<REPO>:ref:refs/heads/main
```

## App Service prerequisites

The workflow deploys a ready-to-run ZIP package. It does **not** provision Azure resources or mutate every app setting for you.

Before the first deployment, make sure App Service is configured with:

- `WEBSITE_RUN_FROM_PACKAGE=1`
- `ASPNETCORE_ENVIRONMENT=Production`
- NoorLocator connection strings and JWT settings
- NoorLocator media settings
- NoorLocator CORS and frontend origin settings

Use these existing documents for the required runtime settings:

- `AZURE_RESOURCES.md`
- `DEPLOYMENT.md`
- `DEPLOYMENT_CHECKLIST.md`

## Branch strategy

Recommended branch strategy for NoorLocator:

- protect `main`
- require pull requests into `main`
- require the GitHub Actions workflow to pass before merge
- let only `main` deploy to the `production` environment
- use GitHub Environment approval rules if you want a manual promotion gate

That keeps feature branches and pull requests as validation-only paths while `main` remains the single production deployment branch.

## Trigger rules

The workflow triggers on:

- `pull_request` to `main`
- `push` to `main`
- `workflow_dispatch`

Markdown-only changes are ignored for the automatic `push` and `pull_request` triggers so documentation edits do not force a deployment run.

## Failure visibility

The workflow is structured so failures are easy to spot:

- build/test/publish/deploy are separated into distinct jobs
- TRX test results are uploaded even when tests fail
- the test job writes a results table to the GitHub step summary
- the publish job fails if the publish folder or ZIP structure is wrong
- the deploy job fails if required Azure configuration is missing
- the deploy job runs post-deployment smoke checks and fails the run if the app does not respond correctly

## Local verification commands

These are the commands used to verify the NoorLocator CI/CD inputs locally before committing the workflow:

```powershell
dotnet restore NoorLocator.sln
dotnet build NoorLocator.sln -c Release --no-restore
dotnet test NoorLocator.sln -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\package-app-service-api.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\verify-app-service-package.ps1
```
