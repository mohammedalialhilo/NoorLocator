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
    Assert-True ($adminPage.Text.Contains('data-auth-roles="Admin"')) "admin.html is missing the admin auth guard metadata."
    Assert-True ($adminPage.Text.Contains("data-logout-action")) "admin.html is missing its logout button."
    $serviceWorkerScript = Send-Request -Method "Get" -Path "/service-worker.js" -Accept "text/javascript"
    Assert-True ($serviceWorkerScript.Text.Contains("NON_CACHEABLE_PATHS")) "service-worker.js is not protecting workspace pages from cache reuse."
    Assert-True ($serviceWorkerScript.Text.Contains('requestUrl.pathname.startsWith("/api/") || NON_CACHEABLE_PATHS.has(requestUrl.pathname)')) "service-worker.js is still caching protected workspace routes."
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

    $createMajlis = Send-Request -Method "Post" -Path "/api/majalis" -Token $managerAuth.Token -Body @{
        title = $majlisTitle
        description = "E2E majlis lifecycle."
        date = [DateTime]::UtcNow.Date.AddDays(14).ToString("o")
        time = "20:00"
        centerId = $managedCenterId
        languageIds = @([int]$assignedCenterLanguages.Json.data[0].id)
    }
    Assert-True ($createMajlis.StatusCode -eq 201) "Manager majlis creation failed."

    $majalisForCenter = Send-Request -Method "Get" -Path "/api/majalis?centerId=$managedCenterId"
    $createdMajlis = @($majalisForCenter.Json.data | Where-Object { $_.title -eq $majlisTitle }) | Select-Object -First 1
    Assert-True ($null -ne $createdMajlis) "Created majlis was not returned from the API."

    $updateMajlis = Send-Request -Method "Put" -Path "/api/majalis/$($createdMajlis.id)" -Token $managerAuth.Token -Body @{
        title = "$majlisTitle Updated"
        description = "E2E majlis updated."
        date = [DateTime]::UtcNow.Date.AddDays(15).ToString("o")
        time = "21:00"
        centerId = $managedCenterId
        languageIds = @([int]$assignedCenterLanguages.Json.data[0].id)
    }
    Assert-True ($updateMajlis.StatusCode -eq 200) "Manager majlis update failed."

    $deleteMajlis = Send-Request -Method "Delete" -Path "/api/majalis/$($createdMajlis.id)" -Token $managerAuth.Token
    Assert-True ($deleteMajlis.StatusCode -eq 200) "Manager majlis delete failed."
    $verification.ManagerMajalis = "Verified manager majlis create, update, and delete flows."

    $announcementForm = [System.Net.Http.MultipartFormDataContent]::new()
    $announcementForm.Add([System.Net.Http.StringContent]::new($announcementTitle), "Title")
    $announcementForm.Add([System.Net.Http.StringContent]::new("E2E manager announcement."), "Description")
    $announcementForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $announcementForm.Add([System.Net.Http.StringContent]::new("Published"), "Status")

    $announcementResponse = Send-Request -Method "Post" -Path "/api/event-announcements" -Token $managerAuth.Token -Content $announcementForm
    Assert-True ($announcementResponse.StatusCode -eq 201) "Manager event announcement creation failed."
    $announcementId = [int]$announcementResponse.Json.data.id

    $imageBytes = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII=")
    $imageForm = [System.Net.Http.MultipartFormDataContent]::new()
    $imageForm.Add([System.Net.Http.StringContent]::new([string]$managedCenterId), "CenterId")
    $imageForm.Add([System.Net.Http.StringContent]::new("false"), "IsPrimary")
    $imageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
    $imageContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
    $imageForm.Add($imageContent, "Image", "e2e-center.png")

    $uploadImage = Send-Request -Method "Post" -Path "/api/center-images/upload" -Token $managerAuth.Token -Content $imageForm
    Assert-True ($uploadImage.StatusCode -eq 201) "Manager center image upload failed."
    $imageId = [int]$uploadImage.Json.data.id

    $setPrimary = Send-Request -Method "Put" -Path "/api/center-images/$imageId/set-primary" -Token $managerAuth.Token
    Assert-True ($setPrimary.StatusCode -eq 200) "Setting the primary center image failed."
    $verification.ManagerAnnouncementsMedia = "Verified manager announcement publishing, center image upload, and primary-image management."

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

    $publicImages = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/images"
    Assert-True ($publicImages.StatusCode -eq 200) "Public center images endpoint failed."
    Assert-True (@($publicImages.Json.data | Where-Object { [int]$_.id -eq $imageId -and $_.isPrimary -eq $true }).Count -ge 1) "Uploaded primary image was not visible publicly."

    $publicMajalis = Send-Request -Method "Get" -Path "/api/centers/$managedCenterId/majalis"
    Assert-True ($publicMajalis.StatusCode -eq 200) "Public center majalis endpoint failed."
    Assert-True ($publicMajalis.Json.data.Count -ge 1) "Public majalis list returned no data."
    $verification.PublicContent = "Verified public display of majalis, event announcements, center images, and center detail content."

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
