param(
    [string]$EnvironmentName = "Production",
    [string]$OutputPath = ".\artifacts\noorlocator-mysql-idempotent.sql",
    [string]$ConnectionString = "",
    [string]$MySqlServerVersion = "8.0.36",
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"

$resolvedProjectRoot = if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}
else {
    (Resolve-Path $ProjectRoot).Path
}

Set-Location $resolvedProjectRoot
$env:ASPNETCORE_ENVIRONMENT = $EnvironmentName

if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
}

if (-not [string]::IsNullOrWhiteSpace($MySqlServerVersion)) {
    $env:MySql__ServerVersion = $MySqlServerVersion
}

$outputDirectory = Split-Path -Path $OutputPath -Parent
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

dotnet ef migrations script `
    --idempotent `
    --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj `
    --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj `
    --output $OutputPath

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef migrations script failed with exit code $LASTEXITCODE."
}
