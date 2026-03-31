param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,
    [string]$ExpectedApiBaseUrl = "",
    [string]$Origin = "",
    [string]$AdminEmail = "",
    [string]$AdminPassword = "",
    [string]$ManagerEmail = "",
    [string]$ManagerPassword = "",
    [int]$UploadCenterId = 0,
    [string]$ExpectedImageUrlPrefix = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$normalizedBaseUrl = $BaseUrl.TrimEnd("/")
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(60)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function Send-Request {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Token = "",
        $Body = $null,
        [System.Net.Http.HttpContent]$Content = $null,
        [string]$Accept = "application/json",
        [string]$RequestOrigin = ""
    )

    $requestUri = if ($Path.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or $Path.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase)) {
        $Path
    }
    else {
        "$normalizedBaseUrl$Path"
    }

    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::$Method, $requestUri)
    if ($Accept) {
        $request.Headers.Accept.ParseAdd($Accept)
    }

    if ($Token) {
        $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
    }

    if ($RequestOrigin) {
        $request.Headers.TryAddWithoutValidation("Origin", $RequestOrigin) | Out-Null
    }

    if ($null -ne $Content) {
        $request.Content = $Content
    }
    elseif ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 10 -Compress
        $request.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
    }

    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $json = $null

    if ($text -and ($text.TrimStart().StartsWith("{") -or $text.TrimStart().StartsWith("["))) {
        try {
            $json = $text | ConvertFrom-Json
        }
        catch {
            $json = $null
        }
    }

    $corsHeader = ""
    if ($response.Headers.Contains("Access-Control-Allow-Origin")) {
        $corsHeader = ($response.Headers.GetValues("Access-Control-Allow-Origin") | Select-Object -First 1)
    }

    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Text = $text
        Json = $json
        ContentType = if ($response.Content.Headers.ContentType) { [string]$response.Content.Headers.ContentType.MediaType } else { "" }
        AccessControlAllowOrigin = $corsHeader
    }
}

function Login-User {
    param(
        [string]$Email,
        [string]$Password
    )

    $response = Send-Request -Method "Post" -Path "/api/auth/login" -Body @{
        email = $Email
        password = $Password
    }

    Assert-True ($response.StatusCode -eq 200) "Login failed for $Email with status $($response.StatusCode)."
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$response.Json.data.token)) "No JWT was returned for $Email."

    [pscustomobject]@{
        Token = [string]$response.Json.data.token
        RefreshToken = [string]$response.Json.data.refreshToken
    }
}

function Resolve-AbsoluteUrl {
    param([string]$Url)

    if ($Url.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or $Url.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Url
    }

    if (-not $Url.StartsWith("/")) {
        return "$normalizedBaseUrl/$Url"
    }

    return "$normalizedBaseUrl$Url"
}

try {
    $summary = [ordered]@{}

    $ping = Send-Request -Method "Get" -Path "/api/health/ping"
    Assert-True ($ping.StatusCode -eq 200) "/api/health/ping did not return HTTP 200."

    $health = Send-Request -Method "Get" -Path "/api/health"
    Assert-True ($health.StatusCode -eq 200) "/api/health did not return HTTP 200."
    $summary.Health = "Verified /api/health/ping and /api/health."

    $runtimeConfig = Send-Request -Method "Get" -Path "/js/runtime-config.js" -Accept "application/javascript"
    Assert-True ($runtimeConfig.StatusCode -eq 200) "/js/runtime-config.js did not return HTTP 200."
    if (-not [string]::IsNullOrWhiteSpace($ExpectedApiBaseUrl)) {
        Assert-True ($runtimeConfig.Text.Contains($ExpectedApiBaseUrl.TrimEnd("/"))) "runtime-config.js did not contain the expected API base URL."
    }
    $summary.RuntimeConfig = "Verified runtime config endpoint."

    $centers = Send-Request -Method "Get" -Path "/api/centers"
    Assert-True ($centers.StatusCode -eq 200) "/api/centers did not return HTTP 200."
    Assert-True ($centers.Json.data.Count -ge 1) "/api/centers returned no data."
    $summary.Database = "Verified /api/centers returns data from the configured database."

    if (-not [string]::IsNullOrWhiteSpace($Origin)) {
        $corsResponse = Send-Request -Method "Get" -Path "/api/centers" -RequestOrigin $Origin
        $normalizedCorsOrigin = if ([string]::IsNullOrWhiteSpace([string]$corsResponse.AccessControlAllowOrigin)) {
            ""
        }
        else {
            [string]$corsResponse.AccessControlAllowOrigin
        }

        $normalizedCorsOrigin = $normalizedCorsOrigin.TrimEnd("/")
        Assert-True ($normalizedCorsOrigin -eq $Origin.TrimEnd("/")) "The configured origin was not echoed in Access-Control-Allow-Origin."
        $summary.Cors = "Verified CORS response for the supplied frontend origin."
    }

    if (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and -not [string]::IsNullOrWhiteSpace($AdminPassword)) {
        $adminAuth = Login-User -Email $AdminEmail -Password $AdminPassword

        $meResponse = Send-Request -Method "Get" -Path "/api/auth/me" -Token $adminAuth.Token
        Assert-True ($meResponse.StatusCode -eq 200) "/api/auth/me did not succeed with the admin JWT."

        $dashboardResponse = Send-Request -Method "Get" -Path "/api/admin/dashboard" -Token $adminAuth.Token
        Assert-True ($dashboardResponse.StatusCode -eq 200) "/api/admin/dashboard did not succeed with the admin JWT."
        $summary.Auth = "Verified admin login, JWT issuance, /api/auth/me, and an admin-protected endpoint."
    }

    if (-not [string]::IsNullOrWhiteSpace($ManagerEmail) -and -not [string]::IsNullOrWhiteSpace($ManagerPassword)) {
        $managerAuth = Login-User -Email $ManagerEmail -Password $ManagerPassword

        $managerCentersResponse = Send-Request -Method "Get" -Path "/api/manager/my-centers" -Token $managerAuth.Token
        Assert-True ($managerCentersResponse.StatusCode -eq 200) "/api/manager/my-centers did not succeed with the manager JWT."

        $resolvedCenterId = $UploadCenterId
        if ($resolvedCenterId -le 0 -and $managerCentersResponse.Json.data.Count -ge 1) {
            $resolvedCenterId = [int]$managerCentersResponse.Json.data[0].id
        }

        if ($resolvedCenterId -gt 0) {
            $imageBytes = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII=")
            $uploadForm = [System.Net.Http.MultipartFormDataContent]::new()
            $uploadForm.Add([System.Net.Http.StringContent]::new([string]$resolvedCenterId), "CenterId")
            $uploadForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
            $imageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
            $imageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
            $uploadForm.Add($imageContent, "Image", "app-service-smoke.png")

            $uploadResponse = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $uploadForm
            Assert-True ($uploadResponse.StatusCode -eq 201) "Manager image upload failed with status $($uploadResponse.StatusCode)."
            Assert-True (-not [string]::IsNullOrWhiteSpace([string]$uploadResponse.Json.data.imageUrl)) "Image upload did not return an image URL."

            $imageUrl = [string]$uploadResponse.Json.data.imageUrl
            if (-not [string]::IsNullOrWhiteSpace($ExpectedImageUrlPrefix)) {
                Assert-True ($imageUrl.StartsWith($ExpectedImageUrlPrefix, [System.StringComparison]::OrdinalIgnoreCase)) "Uploaded image URL did not start with the expected prefix."
            }

            $imageResponse = Send-Request -Method "Get" -Path (Resolve-AbsoluteUrl -Url $imageUrl) -Accept "*/*"
            Assert-True ($imageResponse.StatusCode -eq 200) "Uploaded image URL was not publicly reachable."
            Assert-True ($imageResponse.ContentType.StartsWith("image/")) "Uploaded image did not return an image content type."

            $deleteResponse = Send-Request -Method "Delete" -Path "/api/center-images/$([int]$uploadResponse.Json.data.id)" -Token $managerAuth.Token
            Assert-True ($deleteResponse.StatusCode -eq 200) "Uploaded smoke-test image could not be deleted."
            $summary.Uploads = "Verified manager upload, public image fetch, and cleanup."
        }
        else {
            $summary.Uploads = "Skipped upload verification because no manager center was available."
        }
    }

    [pscustomobject]$summary | ConvertTo-Json -Depth 10
}
finally {
    if ($client) {
        $client.Dispose()
    }
}
