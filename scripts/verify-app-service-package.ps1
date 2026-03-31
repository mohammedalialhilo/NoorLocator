param(
    [string]$ProjectRoot,
    [string]$PublishOutputPath = ".\artifacts\publish\api",
    [string]$ZipPath = ".\artifacts\packages\noorlocator-api-appservice.zip"
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

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path $Path)) {
        throw "$Description was not found at '$Path'."
    }
}

$resolvedProjectRoot = if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}
else {
    (Resolve-Path $ProjectRoot).Path
}

$resolvedPublishOutputPath = Resolve-FullPath -BasePath $resolvedProjectRoot -Path $PublishOutputPath
$resolvedZipPath = Resolve-FullPath -BasePath $resolvedProjectRoot -Path $ZipPath

Assert-PathExists -Path $resolvedPublishOutputPath -Description "Publish output directory"
Assert-PathExists -Path $resolvedZipPath -Description "Deployment package"

$requiredPublishPaths = @(
    @{
        Path = Join-Path $resolvedPublishOutputPath "NoorLocator.Api.dll"
        Description = "Published API assembly"
    },
    @{
        Path = Join-Path (Join-Path $resolvedPublishOutputPath "frontend") "index.html"
        Description = "Published frontend index page"
    },
    @{
        Path = Join-Path (Join-Path (Join-Path $resolvedPublishOutputPath "frontend") "assets") "logo_bkg.png"
        Description = "Published shared branding asset"
    }
)

foreach ($requiredPublishPath in $requiredPublishPaths) {
    Assert-PathExists -Path $requiredPublishPath.Path -Description $requiredPublishPath.Description
}

$publishedUploadsPath = Join-Path (Join-Path $resolvedPublishOutputPath "frontend") "uploads"
if (Test-Path $publishedUploadsPath) {
    $unexpectedPublishedUploads = Get-ChildItem -Path $publishedUploadsPath -Recurse -File | Where-Object { $_.Name -ne ".gitkeep" }
    if ($unexpectedPublishedUploads.Count -gt 0) {
        $firstUnexpectedFile = $unexpectedPublishedUploads | Select-Object -First 1
        throw "Publish output contains unexpected frontend/uploads content: '$($firstUnexpectedFile.FullName)'."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipArchive = [System.IO.Compression.ZipFile]::OpenRead($resolvedZipPath)

try {
    $zipEntries = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $zipArchive.Entries) {
        [void]$zipEntries.Add($entry.FullName.Replace('\', '/'))
    }

    foreach ($requiredEntry in @(
        "NoorLocator.Api.dll",
        "frontend/index.html",
        "frontend/assets/logo_bkg.png"
    )) {
        if (-not $zipEntries.Contains($requiredEntry)) {
            throw "Deployment package is missing the expected entry '$requiredEntry'."
        }
    }

    $unexpectedZipUploads = $zipArchive.Entries |
        Where-Object {
            $_.FullName.StartsWith("frontend/uploads/", [System.StringComparison]::OrdinalIgnoreCase) -and
            -not $_.FullName.EndsWith(".gitkeep", [System.StringComparison]::OrdinalIgnoreCase)
        }

    if ($unexpectedZipUploads) {
        $firstUnexpectedZipEntry = $unexpectedZipUploads | Select-Object -First 1
        throw "Deployment package contains unexpected frontend/uploads content: '$($firstUnexpectedZipEntry.FullName)'."
    }
}
finally {
    $zipArchive.Dispose()
}

Write-Host "Verified publish output path: $resolvedPublishOutputPath"
Write-Host "Verified deployment package path: $resolvedZipPath"
