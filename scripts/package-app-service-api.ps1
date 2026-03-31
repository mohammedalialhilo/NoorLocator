param(
    [string]$Configuration = "Release",
    [string]$ProjectRoot,
    [string]$OutputRoot = ".\artifacts\publish\api",
    [string]$ZipPath = ".\artifacts\packages\noorlocator-api-appservice.zip",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

$resolvedProjectRoot = if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}
else {
    (Resolve-Path $ProjectRoot).Path
}

Set-Location $resolvedProjectRoot

$resolvedOutputRoot = Resolve-FullPath -BasePath $resolvedProjectRoot -Path $OutputRoot
$resolvedZipPath = Resolve-FullPath -BasePath $resolvedProjectRoot -Path $ZipPath
$zipDirectory = Split-Path -Parent $resolvedZipPath
$apiProjectPath = Join-Path (Join-Path $resolvedProjectRoot "NoorLocator.Api") "NoorLocator.Api.csproj"

if (Test-Path $resolvedOutputRoot) {
    Remove-Item -LiteralPath $resolvedOutputRoot -Recurse -Force
}

if (-not (Test-Path $zipDirectory)) {
    New-Item -ItemType Directory -Path $zipDirectory | Out-Null
}

dotnet publish $apiProjectPath `
    -c $Configuration `
    -o $resolvedOutputRoot

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$publishedUploadsRoot = Join-Path (Join-Path $resolvedOutputRoot "frontend") "uploads"
if (Test-Path $publishedUploadsRoot) {
    $publishedUploadFiles = Get-ChildItem -Path $publishedUploadsRoot -Recurse -File | Where-Object { $_.Name -ne ".gitkeep" }
    if ($publishedUploadFiles.Count -gt 0) {
        throw "Publish output still contains frontend/uploads content. Clean the artifact before deploying to App Service."
    }
}

if ($SkipZip) {
    Write-Host "Published NoorLocator API to $resolvedOutputRoot"
    exit 0
}

if (Test-Path $resolvedZipPath) {
    Remove-Item -LiteralPath $resolvedZipPath -Force
}

Compress-Archive -Path (Join-Path $resolvedOutputRoot "*") -DestinationPath $resolvedZipPath

Write-Host "Published NoorLocator API to $resolvedOutputRoot"
Write-Host "Created App Service package at $resolvedZipPath"
