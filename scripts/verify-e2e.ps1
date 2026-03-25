param(
    [string]$BaseUrl = "http://127.0.0.1:5210",
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ConnectionString = "",
    [switch]$StartApp
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$appJob = $null
$clientHandler = [System.Net.Http.HttpClientHandler]::new()
$clientHandler.AllowAutoRedirect = $false
$client = [System.Net.Http.HttpClient]::new($clientHandler)
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
        [string]$Accept = "application/json"
    )

    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::$Method, "$BaseUrl$Path")
    if ($Accept) {
        $request.Headers.Accept.ParseAdd($Accept)
    }

    if ($Token) {
        $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
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

    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        ContentType = if ($response.Content.Headers.ContentType) { [string]$response.Content.Headers.ContentType.MediaType } else { "" }
        Text = $text
        Json = $json
    }
}

function Login-User {
    param([string]$Email, [string]$Password)

    $response = Send-Request -Method "Post" -Path "/api/auth/login" -Body @{
        email = $Email
        password = $Password
    }

    Assert-True ($response.StatusCode -eq 200) "Login failed for $Email with status $($response.StatusCode)."
    Assert-True (-not [string]::IsNullOrWhiteSpace($response.Json.data.token)) "No token returned for $Email."

    [pscustomobject]@{
        Token = [string]$response.Json.data.token
        RefreshToken = [string]$response.Json.data.refreshToken
        Role = [string]$response.Json.data.role
        User = $response.Json.data.user
    }
}

function Wait-ForApp {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        Start-Sleep -Seconds 2
        try {
            $health = Send-Request -Method "Get" -Path "/api/health"
            if ($health.StatusCode -eq 200) {
                return
            }
        }
        catch {
        }
    }

    throw "NoorLocator did not become healthy in time."
}

try {
    if ($StartApp) {
        if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
            throw "ConnectionString is required when -StartApp is used."
        }

        $appJob = Start-Job -ScriptBlock {
            param($projectRoot, $conn, $url)
            Set-Location $projectRoot
            $env:ConnectionStrings__DefaultConnection = $conn
            dotnet run --project .\NoorLocator.Api\NoorLocator.Api.csproj --urls $url
        } -ArgumentList $ProjectRoot, $ConnectionString, $BaseUrl
    }

    Wait-ForApp

    $verification = [ordered]@{}
    $suffix = [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $userEmail = "e2e-$suffix@noorlocator.local"
    $userPassword = "User123!Pass"
    $centerRequestName = "E2E Center $suffix"
    $suggestionMessage = "E2E suggestion $suffix"
    $announcementTitle = "E2E Announcement $suffix"
    $majlisTitle = "E2E Majlis $suffix"
    $imageBytes = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII=")

    $publicPages = @(
        @{ Path = "/"; Expect = "NoorLocator" },
        @{ Path = "/centers.html"; Expect = "Center Directory" },
        @{ Path = "/center-details.html?id=1"; Expect = "Center Details" },
        @{ Path = "/login.html"; Expect = "Login" },
        @{ Path = "/register.html"; Expect = "Register" },
        @{ Path = "/dashboard.html"; Expect = "Dashboard" },
        @{ Path = "/manager.html"; Expect = "Manager" },
        @{ Path = "/admin.html"; Expect = "Admin" },
        @{ Path = "/about"; Expect = "About NoorLocator" },
        @{ Path = "/logout.html"; Expect = "Signing out" }
    )

    foreach ($page in $publicPages) {
        $pageResponse = Send-Request -Method "Get" -Path $page.Path -Accept "text/html"
        Assert-True ($pageResponse.StatusCode -eq 200) "Page $($page.Path) did not return HTTP 200."
        Assert-True ($pageResponse.Text.Contains($page.Expect)) "Page $($page.Path) did not contain expected text '$($page.Expect)'."
    }
    $verification.PublicPages = "Verified public and workspace HTML routes."

    $layoutScript = Send-Request -Method "Get" -Path "/js/layout.js" -Accept "text/javascript"
    Assert-True (($layoutScript.Text.Contains("const attribution =")) -and ($layoutScript.Text.Contains("Copenhagen, Denmark."))) "Global attribution text was not found in layout.js."
    Assert-True ($layoutScript.Text.Contains("data-logout-action")) "Shared logout controls were not found in layout.js."

    $authScript = Send-Request -Method "Get" -Path "/js/auth.js" -Accept "text/javascript"
    Assert-True ($authScript.Text.Contains("sessionStorage.removeItem")) "auth.js does not clear sessionStorage during logout."
    Assert-True ($authScript.Text.Contains("/api/auth/logout")) "auth.js does not call the logout endpoint."
    Assert-True ($authScript.Text.Contains("bootstrapPageAuth")) "auth.js does not expose the centralized page auth bootstrap."
    Assert-True ($authScript.Text.Contains("handleUnauthorized")) "auth.js does not expose centralized unauthorized handling."

    $logoutPage = Send-Request -Method "Get" -Path "/logout.html" -Accept "text/html"
    Assert-True ($logoutPage.Text.Contains("window.NoorLocatorAuth.logout")) "logout.html does not use the shared logout helper."
    Assert-True ($logoutPage.Text.Contains("loggedOut=1")) "logout.html does not redirect with a signed-out flag."
    $dashboardPage = Send-Request -Method "Get" -Path "/dashboard.html" -Accept "text/html"
    $managerPage = Send-Request -Method "Get" -Path "/manager.html" -Accept "text/html"
    $adminPage = Send-Request -Method "Get" -Path "/admin.html" -Accept "text/html"
    Assert-True ($dashboardPage.Text.Contains('data-auth-required="true"')) "dashboard.html is missing the protected-page auth gate."
    Assert-True ($dashboardPage.Text.Contains("data-logout-action")) "dashboard.html is missing its logout button."
    Assert-True ($managerPage.Text.Contains('data-auth-roles="Manager,Admin"')) "manager.html is missing role-aware auth guard metadata."
    Assert-True ($managerPage.Text.Contains("data-logout-action")) "manager.html is missing its logout button."
    Assert-True ($managerPage.Text.Contains("Add a poster or banner image for this majlis.")) "manager.html is missing the majlis image upload field."
    Assert-True ($adminPage.Text.Contains('data-auth-roles="Admin"')) "admin.html is missing the admin auth guard metadata."
    Assert-True ($adminPage.Text.Contains("data-logout-action")) "admin.html is missing its logout button."
    $serviceWorkerScript = Send-Request -Method "Get" -Path "/service-worker.js" -Accept "text/javascript"
    Assert-True ($serviceWorkerScript.Text.Contains("NON_CACHEABLE_PATHS")) "service-worker.js is not protecting workspace pages from cache reuse."
    Assert-True ($serviceWorkerScript.Text.Contains('requestUrl.pathname.startsWith("/api/") || NON_CACHEABLE_PATHS.has(requestUrl.pathname)')) "service-worker.js is still caching protected workspace routes."
    $appScript = Send-Request -Method "Get" -Path "/js/app.js" -Accept "text/javascript"
    Assert-True ($appScript.Text.Contains("majlis-card__image")) "app.js is not rendering majlis images."
    Assert-True ($appScript.Text.Contains("getMajlisImageValidationError")) "app.js is missing majlis image validation."
    $verification.FrontendLogoutAssets = "Verified centralized logout wiring, protected-page auth gates, and protected-page cache exclusions."

    $aboutContent = Send-Request -Method "Get" -Path "/api/content/about"
    Assert-True ($aboutContent.StatusCode -eq 200) "About content API failed."
    Assert-True (($aboutContent.Text.Contains("Driven by")) -and ($aboutContent.Text.Contains("Copenhagen, Denmark."))) "About content attribution did not match the required text pattern."
    $verification.ContentApi = "Verified manifesto content API and attribution payload."

    $register = Send-Request -Method "Post" -Path "/api/auth/register" -Body @{
        name = "End To End User"
        email = $userEmail
        password = $userPassword
    }
    Assert-True ($register.StatusCode -eq 201) "User registration failed."
    $userAuth = Login-User -Email $userEmail -Password $userPassword

    $me = Send-Request -Method "Get" -Path "/api/auth/me" -Token $userAuth.Token
    Assert-True ($me.StatusCode -eq 200) "Authenticated /api/auth/me failed."

    $logout = Send-Request -Method "Post" -Path "/api/auth/logout" -Token $userAuth.Token -Body @{
        refreshToken = $userAuth.RefreshToken
    }
    Assert-True ($logout.StatusCode -eq 200) "Logout endpoint failed."

    $meAfterLogout = Send-Request -Method "Get" -Path "/api/auth/me" -Token $userAuth.Token
    Assert-True ($meAfterLogout.StatusCode -eq 401) "Old JWT still worked after logout."

    $protectedAfterLogout = Send-Request -Method "Get" -Path "/api/center-requests/my" -Token $userAuth.Token
    Assert-True ($protectedAfterLogout.StatusCode -eq 401) "Protected endpoint still worked after logout."
    $verification.AuthLogout = "Verified registration, login, logout, and immediate 401 on protected routes after logout for a user token."

    $userAuth = Login-User -Email $userEmail -Password $userPassword

    $publicCenters = Send-Request -Method "Get" -Path "/api/centers"
    Assert-True ($publicCenters.StatusCode -eq 200) "Public centers list failed."
    Assert-True ($publicCenters.Json.data.Count -ge 1) "No public centers were returned."

    $firstCenter = $publicCenters.Json.data[0]
    $centerId = [int]$firstCenter.id
    $lat = ([decimal]$firstCenter.latitude).ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $lng = ([decimal]$firstCenter.longitude).ToString([System.Globalization.CultureInfo]::InvariantCulture)

    $centersSearch = Send-Request -Method "Get" -Path "/api/centers/search?city=$([uri]::EscapeDataString([string]$firstCenter.city))"
    Assert-True ($centersSearch.StatusCode -eq 200) "Center search failed."

    $centersWithDistance = Send-Request -Method "Get" -Path "/api/centers?lat=$lat&lng=$lng"
    Assert-True ($centersWithDistance.StatusCode -eq 200) "Center list with location context failed."
    Assert-True ($null -ne $centersWithDistance.Json.data[0].distanceKm) "Center list did not include distance when coordinates were supplied."

    $nearestCenters = Send-Request -Method "Get" -Path "/api/centers/nearest?lat=$lat&lng=$lng"
    Assert-True ($nearestCenters.StatusCode -eq 200) "Nearest centers lookup failed."
    Assert-True ($nearestCenters.Json.data.Count -ge 1) "Nearest centers returned no results."
    Assert-True ($null -ne $nearestCenters.Json.data[0].distanceKm) "Nearest centers did not include approximate distance."

    $centerDetail = Send-Request -Method "Get" -Path "/api/centers/$centerId"
    Assert-True ($centerDetail.StatusCode -eq 200) "Center detail failed."

    $centerMajalis = Send-Request -Method "Get" -Path "/api/centers/$centerId/majalis"
    Assert-True ($centerMajalis.StatusCode -eq 200) "Center majalis endpoint failed."

    $centerLanguages = Send-Request -Method "Get" -Path "/api/centers/$centerId/languages"
    Assert-True ($centerLanguages.StatusCode -eq 200) "Center languages endpoint failed."
    $verification.PublicDiscovery = "Verified public center list, search, nearest lookup, detail, majalis, and language endpoints."

    $languages = Send-Request -Method "Get" -Path "/api/languages"
    Assert-True ($languages.StatusCode -eq 200) "Languages endpoint failed."

    $adminAuth = Login-User -Email "admin@noorlocator.local" -Password "Admin123!Pass"
    $managerAuth = Login-User -Email "manager@noorlocator.local" -Password "Manager123!Pass"

    $adminUnauthorized = Send-Request -Method "Get" -Path "/api/admin/dashboard"
    Assert-True ($adminUnauthorized.StatusCode -eq 401) "Anonymous admin dashboard access did not return 401."

    $adminForbidden = Send-Request -Method "Get" -Path "/api/admin/dashboard" -Token $managerAuth.Token
    Assert-True ($adminForbidden.StatusCode -eq 403) "Manager access to admin dashboard did not return 403."
    $verification.Authorization = "Verified 401 and 403 behavior on admin endpoints."

    $managerCenters = Send-Request -Method "Get" -Path "/api/manager/my-centers" -Token $managerAuth.Token
    Assert-True ($managerCenters.StatusCode -eq 200) "Manager my-centers endpoint failed."
    Assert-True ($managerCenters.Json.data.Count -ge 1) "Seeded manager has no assigned centers."
    $managedCenter = $managerCenters.Json.data[0]
    $managedCenterId = [int]$managedCenter.id
    $unmanagedCenter = @($publicCenters.Json.data | Where-Object { [int]$_.id -ne $managedCenterId }) | Select-Object -First 1
    Assert-True ($null -ne $unmanagedCenter) "No unmanaged center was available to verify ownership enforcement."

    $assignedCenterLanguages = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/languages"
    $assignedLanguageIds = @($assignedCenterLanguages.Json.data | ForEach-Object { [int]$_.id })
    $languageSuggestionCenterId = $null
    $availableLanguage = $null

    foreach ($center in $publicCenters.Json.data) {
        $centerLanguagesForSuggestion = Send-Request -Method "Get" -Path "/api/centers/$($center.id)/languages"
        $centerLanguageIds = @($centerLanguagesForSuggestion.Json.data | ForEach-Object { [int]$_.id })
        $candidateLanguage = @($languages.Json.data | Where-Object { $centerLanguageIds -notcontains [int]$_.id }) | Select-Object -First 1

        if ($null -ne $candidateLanguage) {
            $languageSuggestionCenterId = [int]$center.id
            $availableLanguage = $candidateLanguage
            break
        }
    }

    Assert-True ($null -ne $availableLanguage -and $null -ne $languageSuggestionCenterId) "No available language remained for the language suggestion workflow."

    $centerRequest = Send-Request -Method "Post" -Path "/api/center-requests" -Token $userAuth.Token -Body @{
        name = $centerRequestName
        address = "E2E Street 1"
        city = "Malmo"
        country = "Sweden"
        latitude = 55.605
        longitude = 13.0038
        description = "E2E center request."
    }
    Assert-True ($centerRequest.StatusCode -eq 202) "Center request submission failed."

    $suggestion = Send-Request -Method "Post" -Path "/api/suggestions" -Token $userAuth.Token -Body @{
        message = $suggestionMessage
        type = "Feature"
    }
    Assert-True ($suggestion.StatusCode -eq 202) "Suggestion submission failed."

    $languageSuggestion = Send-Request -Method "Post" -Path "/api/center-language-suggestions" -Token $userAuth.Token -Body @{
        centerId = $languageSuggestionCenterId
        languageId = [int]$availableLanguage.id
    }
    Assert-True ($languageSuggestion.StatusCode -eq 202) "Center language suggestion submission failed."

    $managerRequest = Send-Request -Method "Post" -Path "/api/manager/request" -Token $userAuth.Token -Body @{
        centerId = $managedCenterId
    }
    Assert-True ($managerRequest.StatusCode -eq 202) "Manager request submission failed."

    $myRequests = Send-Request -Method "Get" -Path "/api/center-requests/my" -Token $userAuth.Token
    Assert-True ($myRequests.StatusCode -eq 200) "Fetching my center requests failed."
    Assert-True (@($myRequests.Json.data | Where-Object { $_.name -eq $centerRequestName }).Count -ge 1) "Submitted center request was not returned in my requests."
    $verification.UserContribution = "Verified center request, suggestion, center-language suggestion, manager request, and my-requests workflows."

    $createMajlisForm = [System.Net.Http.MultipartFormDataContent]::new()
    $createMajlisForm.Add([System.Net.Http.StringContent]::new($majlisTitle), "Title")
    $createMajlisForm.Add([System.Net.Http.StringContent]::new("E2E majlis lifecycle."), "Description")
    $createMajlisForm.Add([System.Net.Http.StringContent]::new([DateTime]::UtcNow.Date.AddDays(14).ToString("o")), "Date")
    $createMajlisForm.Add([System.Net.Http.StringContent]::new("20:00"), "Time")
    $createMajlisForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $createMajlisForm.Add([System.Net.Http.StringContent]::new([string][int]$assignedCenterLanguages.Json.data[0].id), "LanguageIds")
    $createMajlisImageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
    $createMajlisImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $createMajlisForm.Add($createMajlisImageContent, "Image", "e2e-majlis-create.png")

    $createMajlis = Send-Request -Method "Post" -Path "/api/majalis" -Token $managerAuth.Token -Content $createMajlisForm
    Assert-True ($createMajlis.StatusCode -eq 201) "Manager majlis creation failed."

    $majalisForCenter = Send-Request -Method "Get" -Path "/api/majalis?centerId=$managedCenterId"
    $createdMajlis = @($majalisForCenter.Json.data | Where-Object { $_.title -eq $majlisTitle }) | Select-Object -First 1
    Assert-True ($null -ne $createdMajlis) "Created majlis was not returned from the API."
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$createdMajlis.imageUrl)) "Created majlis did not include an image URL."
    $createdMajlisImageUrl = [string]$createdMajlis.imageUrl

    $createdMajlisImage = Send-Request -Method "Get" -Path $createdMajlisImageUrl -Accept "*/*"
    Assert-True ($createdMajlisImage.StatusCode -eq 200) "Created majlis image was not reachable through static file hosting."
    Assert-True ($createdMajlisImage.ContentType.StartsWith("image/")) "Created majlis image did not return an image content type."

    $updateMajlisForm = [System.Net.Http.MultipartFormDataContent]::new()
    $updateMajlisForm.Add([System.Net.Http.StringContent]::new("$majlisTitle Updated"), "Title")
    $updateMajlisForm.Add([System.Net.Http.StringContent]::new("E2E majlis updated."), "Description")
    $updateMajlisForm.Add([System.Net.Http.StringContent]::new([DateTime]::UtcNow.Date.AddDays(15).ToString("o")), "Date")
    $updateMajlisForm.Add([System.Net.Http.StringContent]::new("21:00"), "Time")
    $updateMajlisForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $updateMajlisForm.Add([System.Net.Http.StringContent]::new([string][int]$assignedCenterLanguages.Json.data[0].id), "LanguageIds")
    $updateMajlisImageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
    $updateMajlisImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $updateMajlisForm.Add($updateMajlisImageContent, "Image", "e2e-majlis-update.png")

    $updateMajlis = Send-Request -Method "Put" -Path "/api/majalis/$($createdMajlis.id)" -Token $managerAuth.Token -Content $updateMajlisForm
    Assert-True ($updateMajlis.StatusCode -eq 200) "Manager majlis update failed."

    $updatedMajlis = Send-Request -Method "Get" -Path "/api/majalis/$($createdMajlis.id)"
    Assert-True ($updatedMajlis.StatusCode -eq 200) "Updated majlis could not be fetched."
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$updatedMajlis.Json.data.imageUrl)) "Updated majlis did not include an image URL."
    Assert-True ([string]$updatedMajlis.Json.data.imageUrl -ne $createdMajlisImageUrl) "Updated majlis image URL did not change after replacement."

    $updatedMajlisImage = Send-Request -Method "Get" -Path ([string]$updatedMajlis.Json.data.imageUrl) -Accept "*/*"
    Assert-True ($updatedMajlisImage.StatusCode -eq 200) "Updated majlis image was not reachable through static file hosting."

    $oldMajlisImageAfterReplace = Send-Request -Method "Get" -Path $createdMajlisImageUrl -Accept "*/*"
    Assert-True ($oldMajlisImageAfterReplace.StatusCode -eq 404) "Replaced majlis image was still reachable."

    $removeMajlisImageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new("$majlisTitle Updated"), "Title")
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new("E2E majlis updated without image."), "Description")
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new([DateTime]::UtcNow.Date.AddDays(16).ToString("o")), "Date")
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new("21:30"), "Time")
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new([string][int]$assignedCenterLanguages.Json.data[0].id), "LanguageIds")
    $removeMajlisImageForm.Add([System.Net.Http.StringContent]::new("true"), "RemoveImage")

    $removeMajlisImage = Send-Request -Method "Put" -Path "/api/majalis/$($createdMajlis.id)" -Token $managerAuth.Token -Content $removeMajlisImageForm
    Assert-True ($removeMajlisImage.StatusCode -eq 200) "Removing a majlis image failed."

    $majlisWithoutImage = Send-Request -Method "Get" -Path "/api/majalis/$($createdMajlis.id)"
    Assert-True ($majlisWithoutImage.StatusCode -eq 200) "Majlis without image could not be fetched."
    Assert-True ([string]::IsNullOrWhiteSpace([string]$majlisWithoutImage.Json.data.imageUrl)) "Majlis image was not cleared."

    $removedUpdatedMajlisImage = Send-Request -Method "Get" -Path ([string]$updatedMajlis.Json.data.imageUrl) -Accept "*/*"
    Assert-True ($removedUpdatedMajlisImage.StatusCode -eq 404) "Removed majlis image was still reachable."

    $deleteMajlis = Send-Request -Method "Delete" -Path "/api/majalis/$($createdMajlis.id)" -Token $managerAuth.Token
    Assert-True ($deleteMajlis.StatusCode -eq 200) "Manager majlis delete failed."
    $verification.ManagerMajalis = "Verified manager majlis create, update, image replacement, image removal, static image reachability, and delete flows."

    $announcementForm = [System.Net.Http.MultipartFormDataContent]::new()
    $announcementForm.Add([System.Net.Http.StringContent]::new($announcementTitle), "Title")
    $announcementForm.Add([System.Net.Http.StringContent]::new("E2E manager announcement."), "Description")
    $announcementForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $announcementForm.Add([System.Net.Http.StringContent]::new("Published"), "Status")

    $announcementResponse = Send-Request -Method "Post" -Path "/api/event-announcements" -Token $managerAuth.Token -Content $announcementForm
    Assert-True ($announcementResponse.StatusCode -eq 201) "Manager event announcement creation failed."
    $announcementId = [int]$announcementResponse.Json.data.id

    $firstImageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $firstImageForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $firstImageForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
    $firstImageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
    $firstImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $firstImageForm.Add($firstImageContent, "Image", "e2e-center-primary-one.png")

    $uploadFirstImage = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $firstImageForm
    Assert-True ($uploadFirstImage.StatusCode -eq 201) "Manager center image upload failed."
    $firstImageId = [int]$uploadFirstImage.Json.data.id

    $invalidImageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $invalidImageForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $invalidImageForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
    $invalidImageContent = [System.Net.Http.ByteArrayContent]::new([System.Text.Encoding]::UTF8.GetBytes("not-an-image"))
    $invalidImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/plain")
    $invalidImageForm.Add($invalidImageContent, "Image", "invalid.txt")

    $invalidImageResponse = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $invalidImageForm
    Assert-True ($invalidImageResponse.StatusCode -eq 400) "Invalid image type did not return HTTP 400."
    Assert-True ($invalidImageResponse.Json.message -eq "Only JPG, JPEG, PNG, and WEBP files are allowed.") "Invalid image type rejection message changed unexpectedly."

    $oversizedBytes = New-Object byte[] ((5 * 1024 * 1024) + 1)
    $oversizedBytes[0] = 0x89
    $oversizedBytes[1] = 0x50
    $oversizedBytes[2] = 0x4E
    $oversizedBytes[3] = 0x47
    $oversizedBytes[4] = 0x0D
    $oversizedBytes[5] = 0x0A
    $oversizedBytes[6] = 0x1A
    $oversizedBytes[7] = 0x0A
    $oversizedImageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $oversizedImageForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $oversizedImageForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
    $oversizedImageContent = [System.Net.Http.ByteArrayContent]::new($oversizedBytes)
    $oversizedImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $oversizedImageForm.Add($oversizedImageContent, "Image", "oversized.png")

    $oversizedImageResponse = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $oversizedImageForm
    Assert-True ($oversizedImageResponse.StatusCode -eq 400) "Oversized image did not return HTTP 400."
    Assert-True ($oversizedImageResponse.Json.message -eq "Image files must be 5MB or smaller.") "Oversized image rejection message changed unexpectedly."

    $forbiddenImageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $forbiddenImageForm.Add([System.Net.Http.StringContent]::new([string]([int]$unmanagedCenter.id)), "CenterId")
    $forbiddenImageForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
    $forbiddenImageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
    $forbiddenImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $forbiddenImageForm.Add($forbiddenImageContent, "Image", "forbidden.png")

    $forbiddenImageResponse = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $forbiddenImageForm
    Assert-True ($forbiddenImageResponse.StatusCode -eq 403) "Uploading to an unmanaged center did not return HTTP 403."
    Assert-True ($forbiddenImageResponse.Json.message -eq "Managers can only manage images for assigned centers.") "Unmanaged-center rejection message changed unexpectedly."

    $secondImageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $secondImageForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $secondImageForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
    $secondImageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
    $secondImageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $secondImageForm.Add($secondImageContent, "Image", "e2e-center-primary-two.png")

    $uploadSecondImage = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $secondImageForm
    Assert-True ($uploadSecondImage.StatusCode -eq 201) "Second manager center image upload failed."
    $secondImageId = [int]$uploadSecondImage.Json.data.id

    $setPrimary = Send-Request -Method "Put" -Path "/api/center-images/$secondImageId/set-primary" -Token $managerAuth.Token
    Assert-True ($setPrimary.StatusCode -eq 200) "Setting the primary center image failed."

    $publicImagesAfterPrimary = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/images"
    Assert-True ($publicImagesAfterPrimary.StatusCode -eq 200) "Public center images endpoint failed after upload."
    Assert-True (@($publicImagesAfterPrimary.Json.data | Where-Object { [int]$_.id -eq $secondImageId -and $_.isPrimary -eq $true }).Count -ge 1) "Uploaded primary image was not visible publicly."
    Assert-True (@($publicImagesAfterPrimary.Json.data | Where-Object { [int]$_.id -eq $firstImageId }).Count -ge 1) "Uploaded gallery image was not visible publicly."

    $publicPrimaryImage = @($publicImagesAfterPrimary.Json.data | Where-Object { [int]$_.id -eq $secondImageId }) | Select-Object -First 1
    $publicPrimaryAsset = Send-Request -Method "Get" -Path ([string]$publicPrimaryImage.imageUrl) -Accept "*/*"
    Assert-True ($publicPrimaryAsset.StatusCode -eq 200) "Uploaded image file was not reachable through static file hosting."
    Assert-True ($publicPrimaryAsset.ContentType.StartsWith("image/")) "Uploaded image did not return an image content type."

    $managerDeleteImage = Send-Request -Method "Delete" -Path "/api/center-images/$firstImageId" -Token $managerAuth.Token
    Assert-True ($managerDeleteImage.StatusCode -eq 200) "Manager could not delete a center image."
    $publicImagesAfterManagerDelete = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/images"
    Assert-True (@($publicImagesAfterManagerDelete.Json.data | Where-Object { [int]$_.id -eq $firstImageId }).Count -eq 0) "Deleted manager image still appeared in the public gallery."

    $adminDeleteImage = Send-Request -Method "Delete" -Path "/api/center-images/$secondImageId" -Token $adminAuth.Token
    Assert-True ($adminDeleteImage.StatusCode -eq 200) "Admin could not moderate-delete a center image."
    $publicImagesAfterAdminDelete = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/images"
    Assert-True (@($publicImagesAfterAdminDelete.Json.data | Where-Object { [int]$_.id -eq $secondImageId }).Count -eq 0) "Admin-deleted image still appeared in the public gallery."
    if ($publicImagesAfterAdminDelete.Json.data.Count -ge 1) {
        Assert-True (@($publicImagesAfterAdminDelete.Json.data | Where-Object { $_.isPrimary -eq $true }).Count -ge 1) "A remaining center gallery did not retain any primary image after deleting the primary image."
    }
    $verification.ManagerAnnouncementsMedia = "Verified manager announcement publishing, reachable image upload endpoint, invalid-type and oversized upload rejection, ownership enforcement, primary-image changes, manager deletion, and admin moderation deletion."

    $adminCenterRequests = Send-Request -Method "Get" -Path "/api/admin/center-requests" -Token $adminAuth.Token
    $centerRequestItem = @($adminCenterRequests.Json.data | Where-Object { $_.name -eq $centerRequestName -and $_.status -eq "Pending" }) | Select-Object -First 1
    Assert-True ($null -ne $centerRequestItem) "Submitted center request was not visible to admin moderation."
    Assert-True ((Send-Request -Method "Post" -Path "/api/admin/center-requests/$($centerRequestItem.id)/approve" -Token $adminAuth.Token).StatusCode -eq 200) "Admin center-request approval failed."

    $adminManagerRequests = Send-Request -Method "Get" -Path "/api/admin/manager-requests" -Token $adminAuth.Token
    $managerRequestItem = @($adminManagerRequests.Json.data | Where-Object { $_.userEmail -eq $userEmail -and [int]$_.centerId -eq $managedCenterId -and $_.status -eq "Pending" }) | Select-Object -First 1
    Assert-True ($null -ne $managerRequestItem) "Submitted manager request was not visible to admin moderation."
    Assert-True ((Send-Request -Method "Post" -Path "/api/admin/manager-requests/$($managerRequestItem.id)/approve" -Token $adminAuth.Token).StatusCode -eq 200) "Admin manager-request approval failed."

    $adminLanguageSuggestions = Send-Request -Method "Get" -Path "/api/admin/center-language-suggestions" -Token $adminAuth.Token
    $languageSuggestionItem = @($adminLanguageSuggestions.Json.data | Where-Object { [int]$_.centerId -eq $languageSuggestionCenterId -and [int]$_.languageId -eq [int]$availableLanguage.id -and $_.status -eq "Pending" }) | Select-Object -First 1
    Assert-True ($null -ne $languageSuggestionItem) "Submitted center-language suggestion was not visible to admin moderation."
    Assert-True ((Send-Request -Method "Post" -Path "/api/admin/center-language-suggestions/$($languageSuggestionItem.id)/approve" -Token $adminAuth.Token).StatusCode -eq 200) "Admin center-language suggestion approval failed."

    $adminSuggestions = Send-Request -Method "Get" -Path "/api/admin/suggestions" -Token $adminAuth.Token
    $suggestionItem = @($adminSuggestions.Json.data | Where-Object { $_.message -eq $suggestionMessage -and $_.status -eq "Pending" }) | Select-Object -First 1
    Assert-True ($null -ne $suggestionItem) "Submitted suggestion was not visible to admin moderation."
    Assert-True ((Send-Request -Method "Put" -Path "/api/admin/suggestions/$($suggestionItem.id)/review" -Token $adminAuth.Token -Content ([System.Net.Http.StringContent]::new(""))).StatusCode -eq 200) "Admin suggestion review failed."

    $adminAudit = Send-Request -Method "Get" -Path "/api/admin/audit-logs" -Token $adminAuth.Token
    Assert-True ($adminAudit.StatusCode -eq 200) "Admin audit logs failed."
    Assert-True ($adminAudit.Json.data.Count -ge 1) "Admin audit log did not return any entries."

    $adminCenters = Send-Request -Method "Get" -Path "/api/admin/centers" -Token $adminAuth.Token
    Assert-True (@($adminCenters.Json.data | Where-Object { $_.name -eq $centerRequestName }).Count -ge 1) "Approved center was not published to admin centers."
    $verification.AdminModeration = "Verified admin moderation approvals, suggestion review, audit log access, and published center creation."

    $upgradedUserAuth = Login-User -Email $userEmail -Password $userPassword
    Assert-True ($upgradedUserAuth.Role -eq "Manager") "User role was not promoted to Manager after approval."

    $newManagerCenters = Send-Request -Method "Get" -Path "/api/manager/my-centers" -Token $upgradedUserAuth.Token
    Assert-True ($newManagerCenters.StatusCode -eq 200) "Approved user could not access manager centers."
    Assert-True (@($newManagerCenters.Json.data | Where-Object { [int]$_.id -eq $managedCenterId }).Count -ge 1) "Approved user did not receive the assigned center."
    $verification.ManagerPromotion = "Verified approved user becomes a manager after re-login and receives assigned center access."

    $publicAnnouncements = Send-Request -Method "Get" -Path "/api/event-announcements?centerId=$managedCenterId"
    Assert-True ($publicAnnouncements.StatusCode -eq 200) "Public announcement feed failed."
    Assert-True (@($publicAnnouncements.Json.data | Where-Object { $_.title -eq $announcementTitle }).Count -ge 1) "Published announcement was not visible publicly."

    $publicCenterPage = Send-Request -Method "Get" -Path "/center-details.html?id=$managedCenterId" -Accept "text/html"
    Assert-True ($publicCenterPage.StatusCode -eq 200) "Public center details page failed."
    Assert-True ($publicCenterPage.Text.Contains("center-hero-image")) "Public center details page is missing the primary image placeholder."
    Assert-True ($publicCenterPage.Text.Contains("center-gallery")) "Public center details page is missing the gallery container."

    $publicMajalis = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/majalis"
    Assert-True ($publicMajalis.StatusCode -eq 200) "Public center majalis endpoint failed."
    Assert-True ($publicMajalis.Json.data.Count -ge 1) "Public majalis list returned no data."
    $verification.PublicContent = "Verified public display of majalis, event announcements, center image API output, static image reachability, and center-detail gallery placeholders."

    $managerLogout = Send-Request -Method "Post" -Path "/api/auth/logout" -Token $managerAuth.Token -Body @{
        refreshToken = $managerAuth.RefreshToken
    }
    Assert-True ($managerLogout.StatusCode -eq 200) "Manager logout endpoint failed."
    Assert-True ((Send-Request -Method "Get" -Path "/api/manager/my-centers" -Token $managerAuth.Token).StatusCode -eq 401) "Manager token still worked after logout."

    $adminLogout = Send-Request -Method "Post" -Path "/api/auth/logout" -Token $adminAuth.Token -Body @{
        refreshToken = $adminAuth.RefreshToken
    }
    Assert-True ($adminLogout.StatusCode -eq 200) "Admin logout endpoint failed."
    Assert-True ((Send-Request -Method "Get" -Path "/api/admin/dashboard" -Token $adminAuth.Token).StatusCode -eq 401) "Admin token still worked after logout."
    $verification.RoleLogout = "Verified manager and admin sessions are revoked and immediately return 401 after logout."

    [pscustomobject]$verification | ConvertTo-Json -Depth 10
}
finally {
    if ($client) {
        $client.Dispose()
    }

    if ($appJob) {
        try {
            Stop-Job -Job $appJob -ErrorAction SilentlyContinue | Out-Null
        }
        catch {
        }

        try {
            Remove-Job -Job $appJob -Force -ErrorAction SilentlyContinue | Out-Null
        }
        catch {
        }
    }
}
