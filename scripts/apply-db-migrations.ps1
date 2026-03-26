param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,
    [string]$EnvironmentName = "Production",
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
$env:ConnectionStrings__DefaultConnection = $ConnectionString
$env:MySql__ServerVersion = $MySqlServerVersion

dotnet ef database update `
    --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj `
    --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef database update failed with exit code $LASTEXITCODE."
}
