param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,
    [string]$ExpectedApiBaseUrl = "",
    [string]$UserEmail = "",
    [string]$UserPassword = "",
    [string]$ManagerEmail = "",
    [string]$ManagerPassword = "",
    [string]$AdminEmail = "",
    [string]$AdminPassword = "",
    [string]$ExpectedImageUrlPrefix = "",
    [string]$EdgePath = ""
)

$ErrorActionPreference = "Stop"

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function Get-AvailableTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Resolve-EdgePath([string]$ConfiguredPath) {
    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        return (Resolve-Path $ConfiguredPath).Path
    }

    $candidates = @(
        "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Microsoft Edge was not found. Install Edge or pass -EdgePath."
}

function Normalize-ExpectedApiBaseUrl([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $normalized = $Value.Trim().TrimEnd("/")
    if ($normalized.EndsWith("/api", [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(0, $normalized.Length - 4)
    }

    return $normalized
}

function Receive-WebSocketJson([System.Net.WebSockets.ClientWebSocket]$Socket) {
    $buffer = New-Object byte[] 8192
    $builder = [System.Text.StringBuilder]::new()

    while ($true) {
        $segment = [System.ArraySegment[byte]]::new($buffer)
        $result = $Socket.ReceiveAsync($segment, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()

        if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
            throw "The browser debugging websocket closed unexpectedly."
        }

        if ($result.Count -gt 0) {
            [void]$builder.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count))
        }

        if ($result.EndOfMessage) {
            break
        }
    }

    $payload = $builder.ToString()
    if ([string]::IsNullOrWhiteSpace($payload)) {
        throw "Received an empty websocket payload from the browser."
    }

    return $payload | ConvertFrom-Json
}

$script:CdpMessageId = 0
function Invoke-Cdp([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Method, [hashtable]$Params = @{}) {
    $script:CdpMessageId++
    $messageId = $script:CdpMessageId
    $payload = @{
        id = $messageId
        method = $Method
        params = $Params
    } | ConvertTo-Json -Compress -Depth 20

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
    $segment = [System.ArraySegment[byte]]::new($bytes)
    [void]$Socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()

    while ($true) {
        $message = Receive-WebSocketJson $Socket

        if ($message.PSObject.Properties.Name -contains "id" -and $message.id -eq $messageId) {
            if ($message.PSObject.Properties.Name -contains "error" -and $null -ne $message.error) {
                throw "CDP command '$Method' failed: $($message.error.message)"
            }

            return $message.result
        }
    }
}

function Invoke-Js([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Expression) {
    $result = Invoke-Cdp $Socket "Runtime.evaluate" @{
        expression = $Expression
        awaitPromise = $true
        returnByValue = $true
    }

    if ($result.PSObject.Properties.Name -contains "exceptionDetails" -and $null -ne $result.exceptionDetails) {
        throw "Browser evaluation failed: $($result.exceptionDetails.text)"
    }

    if ($result.PSObject.Properties.Name -contains "result" -and $result.result.PSObject.Properties.Name -contains "value") {
        return $result.result.value
    }

    return $null
}

function Wait-ForJsCondition([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Expression, [int]$TimeoutSeconds, [string]$FailureMessage) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $value = Invoke-Js $Socket $Expression
            if ($value -eq $true) {
                return
            }
        }
        catch {
            # Ignore transient navigation-time evaluation failures and keep polling.
        }

        Start-Sleep -Milliseconds 300
    }

    throw $FailureMessage
}

function Close-PageSocket([System.Net.WebSockets.ClientWebSocket]$Socket) {
    if ($null -eq $Socket) {
        return
    }

    try {
        $Socket.Dispose()
    }
    catch {
    }
}

function Wait-ForPageTarget([int]$DebugPort, [scriptblock]$Predicate, [int]$TimeoutSeconds, [string]$FailureMessage) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $targets = Invoke-RestMethod -Uri "http://127.0.0.1:$DebugPort/json/list" -TimeoutSec 5
            foreach ($target in @($targets)) {
                if (& $Predicate $target) {
                    return $target
                }
            }
        }
        catch {
        }

        Start-Sleep -Milliseconds 300
    }

    throw $FailureMessage
}

function Open-PageSocket([int]$DebugPort, [scriptblock]$Predicate, [int]$TimeoutSeconds, [string]$FailureMessage) {
    $target = Wait-ForPageTarget $DebugPort $Predicate $TimeoutSeconds $FailureMessage
    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    [void]$socket.ConnectAsync([Uri]$target.webSocketDebuggerUrl, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    [void](Invoke-Cdp $socket "Page.enable")
    [void](Invoke-Cdp $socket "Runtime.enable")
    return $socket
}

function Navigate-To([ref]$SocketRef, [int]$DebugPort, [string]$AbsoluteUrl) {
    $previousUrl = ""
    try {
        $previousUrl = [string](Invoke-Js $SocketRef.Value "location.href")
    }
    catch {
    }

    [void](Invoke-Cdp $SocketRef.Value "Page.navigate" @{ url = $AbsoluteUrl })

    $absoluteUrlJson = $AbsoluteUrl | ConvertTo-Json -Compress
    $navigationCompletedOnCurrentSocket = $false
    try {
        Wait-ForJsCondition $SocketRef.Value "window.location.href === $absoluteUrlJson && document.readyState === 'complete'" 10 "Timed out waiting for $AbsoluteUrl to finish loading."
        $navigationCompletedOnCurrentSocket = $true
    }
    catch {
    }

    if (-not $navigationCompletedOnCurrentSocket) {
        Close-PageSocket $SocketRef.Value
        $SocketRef.Value = Open-PageSocket $DebugPort {
            param($target)

            if ($target.type -ne "page" -or [string]::IsNullOrWhiteSpace($target.webSocketDebuggerUrl)) {
                return $false
            }

            try {
                return [string]::Equals(([Uri]$target.url).AbsoluteUri, $AbsoluteUrl, [System.StringComparison]::OrdinalIgnoreCase)
            }
            catch {
                if ([string]::IsNullOrWhiteSpace($previousUrl)) {
                    return $true
                }

                return -not [string]::Equals($target.url, $previousUrl, [System.StringComparison]::OrdinalIgnoreCase)
            }
        } 20 "Timed out waiting for $AbsoluteUrl to finish loading."
        Wait-ForJsCondition $SocketRef.Value "document.readyState === 'complete'" 20 "Timed out waiting for $AbsoluteUrl to finish loading."
    }
}

function Join-BaseUrl([string]$Root, [string]$Path) {
    if ($Path.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or $Path.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path
    }

    return "$($Root.TrimEnd('/'))$Path"
}

function Assert-Branding([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$PageLabel) {
    $state = Invoke-Js $Socket @"
(() => ({
  logoCount: document.querySelectorAll('[data-brand-logo]').length,
  logosLoaded: Array.from(document.querySelectorAll('[data-brand-logo]')).every(image => image.complete && image.naturalWidth > 0),
  faviconHref: document.querySelector('link[rel="icon"]')?.getAttribute('href') || '',
  footerText: document.querySelector('[data-site-footer]')?.innerText || '',
  bodyText: document.body?.innerText || ''
}))()
"@

    Assert-True ($state.logoCount -ge 1) "$PageLabel did not render any NoorLocator brand logos."
    Assert-True ($state.logosLoaded -eq $true) "$PageLabel rendered a broken NoorLocator logo image."
    Assert-True ($state.faviconHref -like "*assets/logo_bkg.png*") "$PageLabel is not using frontend/assets/logo_bkg.png as the favicon."
    Assert-True ($state.footerText -like "*Copenhagen, Denmark.*") "$PageLabel footer branding did not include the NoorLocator attribution."
}

function Login-ThroughUi([ref]$SocketRef, [int]$DebugPort, [string]$RootUrl, [string]$Email, [string]$Password, [string]$ExpectedPath, [string]$ReadyExpression, [string]$ReadyMessage, [string]$FailureLabel) {
    Navigate-To $SocketRef $DebugPort (Join-BaseUrl $RootUrl "/login.html")
    Wait-ForJsCondition $SocketRef.Value "!!window.NoorLocatorApi && !!document.getElementById('login-form')" 20 "The login page did not initialize correctly."

    $emailJson = $Email | ConvertTo-Json -Compress
    $passwordJson = $Password | ConvertTo-Json -Compress
    [void](Invoke-Js $SocketRef.Value @"
(() => {
  document.getElementById('login-email').value = $emailJson;
  document.getElementById('login-password').value = $passwordJson;
  document.getElementById('login-form').requestSubmit();
  return true;
})()
"@)

    $loginCompletedOnCurrentSocket = $false
    try {
        Wait-ForJsCondition $SocketRef.Value "window.location.pathname.endsWith('$ExpectedPath')" 12 "$FailureLabel did not redirect to $ExpectedPath after login."
        Wait-ForJsCondition $SocketRef.Value $ReadyExpression 25 $ReadyMessage
        $loginCompletedOnCurrentSocket = $true
    }
    catch {
    }

    if (-not $loginCompletedOnCurrentSocket) {
        Close-PageSocket $SocketRef.Value
        $SocketRef.Value = Open-PageSocket $DebugPort {
            param($target)

            if ($target.type -ne "page" -or [string]::IsNullOrWhiteSpace($target.url)) {
                return $false
            }

            try {
                $targetUri = [Uri]$target.url
                return $targetUri.AbsolutePath.EndsWith($ExpectedPath, [System.StringComparison]::OrdinalIgnoreCase)
            }
            catch {
                return $false
            }
        } 25 "$FailureLabel did not redirect to $ExpectedPath after login."
        Wait-ForJsCondition $SocketRef.Value $ReadyExpression 25 $ReadyMessage
    }
}

function Logout-ThroughUi([ref]$SocketRef, [int]$DebugPort, [string]$FailureLabel) {
    [void](Invoke-Js $SocketRef.Value @"
(() => {
  const button = document.querySelector('[data-logout-action]');
  if (!button) {
    throw new Error('Logout control was not found.');
  }

  button.click();
  return true;
})()
"@)

    $postLogoutLocation = ""
    $logoutCompletedOnCurrentSocket = $false
    try {
        Wait-ForJsCondition $SocketRef.Value "window.location.pathname.endsWith('/login.html')" 12 "$FailureLabel did not return to the login page after logout."
        $postLogoutLocation = [string](Invoke-Js $SocketRef.Value "window.location.pathname + window.location.search")
        $logoutCompletedOnCurrentSocket = $true
    }
    catch {
    }

    if (-not $logoutCompletedOnCurrentSocket) {
        $deadline = (Get-Date).AddSeconds(25)

        while ((Get-Date) -lt $deadline) {
            Close-PageSocket $SocketRef.Value

            try {
                $SocketRef.Value = Open-PageSocket $DebugPort {
                    param($target)
                    $target.type -eq "page" -and -not [string]::IsNullOrWhiteSpace($target.webSocketDebuggerUrl)
                } 5 "$FailureLabel did not expose a browser page after logout."

                try {
                    Wait-ForJsCondition $SocketRef.Value "document.readyState === 'complete'" 3 "Timed out waiting for the logout redirect to settle."
                }
                catch {
                    # Ignore transient readiness failures while the logout redirect is still in flight.
                }

                $postLogoutLocation = [string](Invoke-Js $SocketRef.Value "window.location.pathname + window.location.search")
                if ($postLogoutLocation.StartsWith("/login.html", [System.StringComparison]::OrdinalIgnoreCase)) {
                    break
                }
            }
            catch {
                # The page target can disappear during logout redirects. Keep retrying until login is stable.
            }

            Start-Sleep -Milliseconds 300
        }
    }

    Assert-True ($postLogoutLocation.StartsWith("/login.html", [System.StringComparison]::OrdinalIgnoreCase)) "$FailureLabel did not return to the login page after logout."
    Wait-ForJsCondition $SocketRef.Value @"
(() => !localStorage.getItem('noorlocator.auth.token')
  && !localStorage.getItem('noorlocator.auth.refreshToken')
  && !localStorage.getItem('noorlocator.auth.user')
  && !sessionStorage.getItem('noorlocator.auth.token')
  && !sessionStorage.getItem('noorlocator.auth.refreshToken')
  && !sessionStorage.getItem('noorlocator.auth.user'))()
"@ 10 "$FailureLabel did not clear the stored authentication session."
}

$normalizedBaseUrl = $BaseUrl.TrimEnd("/")
$normalizedExpectedApiBaseUrl = Normalize-ExpectedApiBaseUrl $ExpectedApiBaseUrl
$edgeExecutable = Resolve-EdgePath $EdgePath
$debugPort = Get-AvailableTcpPort
$userDataDir = Join-Path ([System.IO.Path]::GetTempPath()) ("noorlocator-frontend-smoke-" + [Guid]::NewGuid().ToString("N"))
$browserProcess = $null
$webSocket = $null

try {
    New-Item -ItemType Directory -Path $userDataDir | Out-Null

    $browserProcess = Start-Process -FilePath $edgeExecutable -ArgumentList @(
        "--headless=new",
        "--disable-gpu",
        "--no-first-run",
        "--no-default-browser-check",
        "--remote-debugging-port=$debugPort",
        "--user-data-dir=$userDataDir",
        "about:blank"
    ) -PassThru

    $debugReady = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        Start-Sleep -Milliseconds 500

        if ($browserProcess.HasExited) {
            throw "Microsoft Edge exited before the remote debugging endpoint became available."
        }

        try {
            $targets = Invoke-RestMethod -Uri "http://127.0.0.1:$debugPort/json/list" -TimeoutSec 5
            if (@($targets | Where-Object { $_.type -eq "page" }).Count -ge 1) {
                $debugReady = $true
                break
            }
        }
        catch {
        }
    }

    if (-not $debugReady) {
        throw "Microsoft Edge remote debugging did not expose a page target in time."
    }

    $webSocket = Open-PageSocket $debugPort {
        param($target)
        $target.type -eq "page" -and -not [string]::IsNullOrWhiteSpace($target.webSocketDebuggerUrl)
    } 10 "Microsoft Edge did not expose a usable page target."

    $summary = [ordered]@{}

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/")
    Wait-ForJsCondition $webSocket "!!window.NoorLocatorConfig && !!window.NoorLocatorApi && document.querySelectorAll('#featured-centers article.card').length >= 1" 25 "The home page did not finish loading public center content."
    Assert-Branding $webSocket "The home page"
    $runtimeState = Invoke-Js $webSocket @"
(() => ({
  runtimeApiBaseUrl: window.NoorLocatorRuntimeConfig?.apiBaseUrl || '',
  homeStatus: document.getElementById('home-status')?.textContent || ''
}))()
"@
    if (-not [string]::IsNullOrWhiteSpace($normalizedExpectedApiBaseUrl)) {
        Assert-True ($runtimeState.runtimeApiBaseUrl -eq $normalizedExpectedApiBaseUrl) "runtime-config.js exposed '$($runtimeState.runtimeApiBaseUrl)' instead of '$normalizedExpectedApiBaseUrl'."
    }
    Assert-True ($runtimeState.homeStatus -notlike "*could not*" -and $runtimeState.homeStatus -notlike "*unable*" -and $runtimeState.homeStatus -notlike "*error*") "The home page reported an error instead of a successful public load."
    $summary.PublicHome = "Verified the home page, runtime-config API base URL, logo path, and footer branding."

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/about")
    Wait-ForJsCondition $webSocket "document.getElementById('about-page-message')?.textContent.includes('loaded successfully')" 25 "The About page did not finish loading manifesto content."
    Assert-Branding $webSocket "The About page"
    $summary.PublicAbout = "Verified the About page content flow and retained branding."

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/centers.html")
    Wait-ForJsCondition $webSocket "document.querySelectorAll('#centers-list article.card').length >= 1" 25 "The centers directory did not render any public centers."
    Assert-Branding $webSocket "The centers page"
    $summary.PublicDirectory = "Verified the public centers directory loads from the live API."

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/dashboard.html")
    Wait-ForJsCondition $webSocket "window.location.pathname.endsWith('/login.html')" 15 "An anonymous visit to dashboard.html did not redirect to login.html."
    $summary.AuthGuard = "Verified protected dashboard routing redirects anonymous users to login."

    if (-not [string]::IsNullOrWhiteSpace($UserEmail) -and -not [string]::IsNullOrWhiteSpace($UserPassword)) {
        Login-ThroughUi ([ref]$webSocket) $debugPort $normalizedBaseUrl $UserEmail $UserPassword "/dashboard.html" "document.body.dataset.authReady === 'true' && document.getElementById('dashboard-page-message')?.textContent.includes('Your contribution tools are ready')" "The user dashboard did not finish loading after login." "The user flow"
        Assert-Branding $webSocket "The user dashboard"
        Logout-ThroughUi ([ref]$webSocket) $debugPort "The user flow"
        Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/dashboard.html")
        Wait-ForJsCondition $webSocket "window.location.pathname.endsWith('/login.html')" 15 "The dashboard was still reachable after logout."
        $summary.UserAuth = "Verified login, user dashboard bootstrap, logout, and post-logout protection."
    }

    if (-not [string]::IsNullOrWhiteSpace($ManagerEmail) -and -not [string]::IsNullOrWhiteSpace($ManagerPassword)) {
        Login-ThroughUi ([ref]$webSocket) $debugPort $normalizedBaseUrl $ManagerEmail $ManagerPassword "/manager.html" "document.body.dataset.authReady === 'true' && document.getElementById('manager-page-message')?.textContent.includes('Manager workspace loaded from the live API')" "The manager workspace did not finish loading after login." "The manager flow"
        Assert-Branding $webSocket "The manager workspace"

        $uploadResult = Invoke-Js $webSocket @"
(async () => {
  const centersResponse = await window.NoorLocatorApi.getManagerCenters();
  const centerId = centersResponse.data?.[0]?.id;
  if (!centerId) {
    throw new Error('No assigned manager center was available for frontend smoke testing.');
  }

  const bytes = Uint8Array.from(atob('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII='), character => character.charCodeAt(0));
  const formData = new FormData();
  formData.set('centerId', String(centerId));
  formData.set('isPrimary', 'true');
  formData.set('image', new File([bytes], 'frontend-smoke.png', { type: 'image/png' }));

  const response = await window.NoorLocatorApi.uploadCenterImage(formData);
  return {
    centerId: response.data.id ? centerId : 0,
    imageId: response.data.id,
    imageUrl: response.data.imageUrl || ''
  };
})()
"@

        Assert-True ($uploadResult.centerId -gt 0) "The manager frontend smoke test could not determine a center for image verification."
        Assert-True (-not [string]::IsNullOrWhiteSpace($uploadResult.imageUrl)) "The manager frontend smoke test upload did not return an image URL."
        if (-not [string]::IsNullOrWhiteSpace($ExpectedImageUrlPrefix)) {
            Assert-True ($uploadResult.imageUrl.StartsWith($ExpectedImageUrlPrefix, [System.StringComparison]::OrdinalIgnoreCase)) "The uploaded image URL '$($uploadResult.imageUrl)' did not start with '$ExpectedImageUrlPrefix'."
        }

        Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/center-details.html?id=$($uploadResult.centerId)")
        Wait-ForJsCondition $webSocket "document.getElementById('center-detail-message')?.textContent.includes('loaded successfully')" 25 "The center details page did not finish loading after the manager upload."
        Wait-ForJsCondition $webSocket @"
(() => {
  const heroImage = document.getElementById('center-hero-image');
  return !!heroImage && !heroImage.hidden && heroImage.complete && heroImage.naturalWidth > 0;
})()
"@ 25 "The public center hero image did not render successfully."
        $imageState = Invoke-Js $webSocket @"
(() => ({
  heroImageSrc: document.getElementById('center-hero-image')?.src || '',
  detailMessage: document.getElementById('center-detail-message')?.textContent || ''
}))()
"@
        $expectedRenderedImageUrl = if ($uploadResult.imageUrl.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or $uploadResult.imageUrl.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase)) {
            $uploadResult.imageUrl
        }
        else {
            Join-BaseUrl $normalizedBaseUrl $uploadResult.imageUrl
        }

        Assert-True ($imageState.heroImageSrc -eq $expectedRenderedImageUrl) "The public center page did not render the uploaded production image URL."
        Assert-Branding $webSocket "The public center details page"

        [void](Invoke-Js $webSocket @"
(async () => {
  await window.NoorLocatorApi.deleteCenterImage($($uploadResult.imageId));
  return true;
})()
"@)

        Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/manager.html")
        Wait-ForJsCondition $webSocket "document.body.dataset.authReady === 'true' && document.getElementById('manager-page-message')?.textContent.length > 0" 20 "The manager workspace did not recover after image cleanup."
        Logout-ThroughUi ([ref]$webSocket) $debugPort "The manager flow"
        $summary.ManagerFlow = "Verified manager login, manager dashboard bootstrap, uploaded-image rendering on the public center page, and logout."
    }

    if (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and -not [string]::IsNullOrWhiteSpace($AdminPassword)) {
        Login-ThroughUi ([ref]$webSocket) $debugPort $normalizedBaseUrl $AdminEmail $AdminPassword "/admin.html" "document.body.dataset.authReady === 'true' && document.getElementById('admin-page-message')?.textContent.includes('Admin workspace loaded from the secured API')" "The admin workspace did not finish loading after login." "The admin flow"
        Assert-Branding $webSocket "The admin workspace"
        Wait-ForJsCondition $webSocket "document.querySelectorAll('#admin-cards .card, #admin-cards article').length >= 1" 15 "The admin dashboard summary cards did not render."
        Logout-ThroughUi ([ref]$webSocket) $debugPort "The admin flow"
        $summary.AdminFlow = "Verified admin login, admin dashboard bootstrap, and logout."
    }

    [pscustomobject]$summary | ConvertTo-Json -Depth 10
}
finally {
    if ($webSocket) {
        try {
            $webSocket.Dispose()
        }
        catch {
        }
    }

    if ($browserProcess -and -not $browserProcess.HasExited) {
        try {
            Stop-Process -Id $browserProcess.Id -Force
        }
        catch {
        }
    }

    if (Test-Path $userDataDir) {
        try {
            Remove-Item -LiteralPath $userDataDir -Recurse -Force
        }
        catch {
        }
    }
}
