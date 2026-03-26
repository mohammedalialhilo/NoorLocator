param(
    [string]$EnvironmentName = "Production",
    [string]$OutputPath = ".\artifacts\noorlocator-mysql-idempotent.sql",
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

$outputDirectory = Split-Path -Path $OutputPath -Parent
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

dotnet ef migrations script `
    --idempotent `
    --project .\NoorLocator.Infrastructure\NoorLocator.Infrastructure.csproj `
    --startup-project .\NoorLocator.Api\NoorLocator.Api.csproj `
    --output $OutputPath
