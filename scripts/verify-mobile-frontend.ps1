param(
    [string]$BaseUrl = "http://127.0.0.1:5210",
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ConnectionString = "",
    [switch]$StartApp,
    [string]$UserEmail = "user@noorlocator.local",
    [string]$UserPassword = "User123!Pass",
    [string]$ManagerEmail = "manager@noorlocator.local",
    [string]$ManagerPassword = "Manager123!Pass",
    [string]$AdminEmail = "admin@noorlocator.local",
    [string]$AdminPassword = "Admin123!Pass",
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

function Wait-ForApp([string]$RootUrl) {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        Start-Sleep -Seconds 2
        try {
            $health = Invoke-RestMethod -Uri "$($RootUrl.TrimEnd('/'))/api/health" -TimeoutSec 10
            if ($null -ne $health) {
                return
            }
        }
        catch {
        }
    }

    throw "NoorLocator did not become healthy in time."
}

function Get-FirstCenterId([string]$RootUrl) {
    $response = Invoke-RestMethod -Uri "$($RootUrl.TrimEnd('/'))/api/centers" -TimeoutSec 30
    $centers = @($response.data)
    Assert-True ($centers.Count -ge 1) "The mobile verification could not find any public centers."
    return [int]$centers[0].id
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
        $snippet = $Expression.Trim()
        if ($snippet.Length -gt 160) {
            $snippet = $snippet.Substring(0, 160)
        }

        $exceptionText = $result.exceptionDetails.exception.description
        if ([string]::IsNullOrWhiteSpace($exceptionText)) {
            $exceptionText = $result.exceptionDetails.text
        }

        throw "Browser evaluation failed: $exceptionText | Expression: $snippet"
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

function Install-TestHooks([System.Net.WebSockets.ClientWebSocket]$Socket) {
    [void](Invoke-Cdp $Socket "Page.addScriptToEvaluateOnNewDocument" @{
        source = @"
(() => {
  window.__noorlocatorConsoleErrors = [];

  const stringifyArgs = (args) => args
    .map(value => {
      if (typeof value === 'string') {
        return value;
      }

      try {
        return JSON.stringify(value);
      } catch {
        return String(value);
      }
    })
    .join(' ');

  const originalConsoleError = console.error.bind(console);
  console.error = (...args) => {
    window.__noorlocatorConsoleErrors.push(stringifyArgs(args));
    return originalConsoleError(...args);
  };

  window.addEventListener('error', event => {
    window.__noorlocatorConsoleErrors.push(event.message || 'Unhandled window error');
  });

  window.addEventListener('unhandledrejection', event => {
    const reason = event.reason;
    window.__noorlocatorConsoleErrors.push(typeof reason === 'string' ? reason : (reason?.message || 'Unhandled promise rejection'));
  });

  window.__noorlocatorClearDiagnostics = () => {
    window.__noorlocatorConsoleErrors = [];
  };
})();
"@
    })
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
    Install-TestHooks $socket
    return $socket
}

$normalizedBaseUrl = $BaseUrl.TrimEnd("/")
$centerId = 0
$appJob = $null
$edgeExecutable = Resolve-EdgePath $EdgePath
$debugPort = Get-AvailableTcpPort
$userDataDir = Join-Path ([System.IO.Path]::GetTempPath()) ("noorlocator-mobile-smoke-" + [Guid]::NewGuid().ToString("N"))
$browserProcess = $null
$webSocket = $null
$tempUserSuffix = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$tempUserEmail = "mobile-$tempUserSuffix@noorlocator.local"
$tempUserPassword = "User123!Pass"

function Join-BaseUrl([string]$Root, [string]$Path) {
    if ($Path.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or $Path.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path
    }

    return "$($Root.TrimEnd('/'))$Path"
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
        Wait-ForJsCondition $SocketRef.Value "window.location.href === $absoluteUrlJson && document.readyState === 'complete'" 12 "Timed out waiting for $AbsoluteUrl to finish loading."
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

function Click-AndWaitForPath([ref]$SocketRef, [int]$DebugPort, [string]$ClickExpression, [string]$ExpectedPath, [int]$TimeoutSeconds, [string]$FailureMessage) {
    [void](Invoke-Js $SocketRef.Value $ClickExpression)

    $navigationCompletedOnCurrentSocket = $false
    try {
        Wait-ForJsCondition $SocketRef.Value "window.location.pathname.endsWith('$ExpectedPath') && document.readyState === 'complete'" $TimeoutSeconds $FailureMessage
        $navigationCompletedOnCurrentSocket = $true
    }
    catch {
    }

    if (-not $navigationCompletedOnCurrentSocket) {
        Close-PageSocket $SocketRef.Value
        $SocketRef.Value = Open-PageSocket $DebugPort {
            param($target)

            if ($target.type -ne "page" -or [string]::IsNullOrWhiteSpace($target.url)) {
                return $false
            }

            try {
                return ([Uri]$target.url).AbsolutePath.EndsWith($ExpectedPath, [System.StringComparison]::OrdinalIgnoreCase)
            }
            catch {
                return $false
            }
        } $TimeoutSeconds $FailureMessage
        Wait-ForJsCondition $SocketRef.Value "document.readyState === 'complete'" 20 "Timed out waiting for $ExpectedPath to finish loading."
    }
}

function Set-Viewport([System.Net.WebSockets.ClientWebSocket]$Socket, [int]$Width, [int]$Height, [bool]$IsMobile) {
    [void](Invoke-Cdp $Socket "Emulation.setDeviceMetricsOverride" @{
        width = $Width
        height = $Height
        deviceScaleFactor = 1
        mobile = $IsMobile
        screenWidth = $Width
        screenHeight = $Height
    })

    [void](Invoke-Cdp $Socket "Emulation.setTouchEmulationEnabled" @{
        enabled = $IsMobile
        maxTouchPoints = if ($IsMobile) { 5 } else { 1 }
    })
}

function Assert-NoConsoleErrors([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    $errors = @(
        Invoke-Js $Socket @"
(() => Array.isArray(window.__noorlocatorConsoleErrors) ? window.__noorlocatorConsoleErrors : [])()
"@
    )

    Assert-True ($errors.Count -eq 0) "$Label logged console errors: $($errors -join ' | ')"
}

function Assert-ResponsiveLayout([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    $report = Invoke-Js $Socket @'
(() => {
  const doc = document.documentElement;
  const offenders = Array.from(document.querySelectorAll('body *'))
    .filter(element => {
      if (element.classList?.contains('site-nav-scrim') || element.classList?.contains('sr-only')) {
        return false;
      }

      if (element.closest('thead')) {
        return false;
      }

      const style = window.getComputedStyle(element);
      if (style.display === 'none' || style.visibility === 'hidden') {
        return false;
      }

      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.left >= -1 && rect.right > window.innerWidth + 2;
    })
    .slice(0, 6)
    .map(element => {
      const rect = element.getBoundingClientRect();
      const selector = [
        element.tagName.toLowerCase(),
        element.id ? '#' + element.id : '',
        element.className ? '.' + String(element.className).trim().replace(/\s+/g, '.') : ''
      ].join('');
      return `${selector} (${Math.round(rect.right - window.innerWidth)}px overflow)`;
    });

  const unreadableText = Array.from(document.querySelectorAll('p, li, a, button, label, input, textarea, select, span'))
    .filter(element => {
      const style = window.getComputedStyle(element);
      if (style.display === 'none' || style.visibility === 'hidden') {
        return false;
      }

      const rect = element.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0) {
        return false;
      }

      return parseFloat(style.fontSize) < 12;
    })
    .slice(0, 6)
    .map(element => `${element.tagName.toLowerCase()}:${window.getComputedStyle(element).fontSize}`);

  return {
    scrollWidth: doc.scrollWidth,
    viewportWidth: window.innerWidth,
    offenders,
    unreadableText
  };
})()
'@

    Assert-True ($report.scrollWidth -le ($report.viewportWidth + 2)) "$Label overflowed horizontally: scrollWidth=$($report.scrollWidth), viewportWidth=$($report.viewportWidth)."
    Assert-True (@($report.offenders).Count -eq 0) "$Label had elements overflowing the viewport: $(@($report.offenders) -join ' | ')"
    Assert-True (@($report.unreadableText).Count -eq 0) "$Label rendered unreadably small text: $(@($report.unreadableText) -join ' | ')"
}

function Assert-TouchTargets([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Selector, [string]$Label) {
    $report = Invoke-Js $Socket @"
(() => {
  const visibleItems = Array.from(document.querySelectorAll('$Selector'))
    .filter(element => {
      const style = window.getComputedStyle(element);
      const rect = element.getBoundingClientRect();
      return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
    })
    .map(element => {
      const rect = element.getBoundingClientRect();
      const text = (element.innerText || element.value || element.getAttribute('aria-label') || element.tagName || '').trim().slice(0, 48);
      return {
        text,
        width: Math.round(rect.width),
        height: Math.round(rect.height)
      };
    });

  return {
    count: visibleItems.length,
    tooSmall: visibleItems.filter(item => item.width < 44 || item.height < 44).slice(0, 6)
  };
})()
"@

    Assert-True ($report.count -ge 1) "$Label did not render any visible touch targets for selector '$Selector'."
    Assert-True (@($report.tooSmall).Count -eq 0) "$Label rendered touch targets smaller than 44x44: $((@($report.tooSmall) | ForEach-Object { $name = if ([string]::IsNullOrWhiteSpace($_.text)) { '<unnamed>' } else { $_.text }; $name + ' ' + $_.width + 'x' + $_.height }) -join ' | ')"
}

function Assert-FormsFitViewport([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    $report = Invoke-Js $Socket @"
(() => {
  const offenders = Array.from(document.querySelectorAll('input, textarea, select'))
    .filter(element => {
      const style = window.getComputedStyle(element);
      if (style.display === 'none' || style.visibility === 'hidden') {
        return false;
      }

      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.right > window.innerWidth + 2;
    })
    .slice(0, 6)
    .map(element => element.name || element.id || element.tagName.toLowerCase());

  return {
    inputCount: Array.from(document.querySelectorAll('input, textarea, select')).length,
    offenders
  };
})()
"@

    Assert-True ($report.inputCount -ge 1) "$Label did not render any form fields."
    Assert-True (@($report.offenders).Count -eq 0) "$Label had form controls overflowing the viewport: $(@($report.offenders) -join ', ')"
}

function Assert-MobileNavVisible([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    $state = Invoke-Js $Socket @"
(() => {
  const toggle = document.querySelector('[data-nav-toggle]');
  if (!toggle) {
    return { exists: false, visible: false, width: 0, height: 0 };
  }

  const rect = toggle.getBoundingClientRect();
  const style = window.getComputedStyle(toggle);
  return {
    exists: true,
    visible: style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0,
    width: Math.round(rect.width),
    height: Math.round(rect.height)
  };
})()
"@

    Assert-True ($state.exists -eq $true) "$Label did not render the hamburger toggle."
    Assert-True ($state.visible -eq $true) "$Label did not show the hamburger toggle on the mobile viewport."
    Assert-True ($state.width -ge 44 -and $state.height -ge 44) "$Label rendered a hamburger toggle smaller than 44x44."
}

function Open-MobileMenu([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    Assert-MobileNavVisible $Socket $Label
    [void](Invoke-Js $Socket "document.querySelector('[data-nav-toggle]').click(); true")
    Wait-ForJsCondition $Socket @"
(() => document.querySelector('[data-nav-toggle]')?.getAttribute('aria-expanded') === 'true'
  && document.querySelector('[data-nav-panel]')?.classList.contains('is-open')
  && document.body.classList.contains('nav-open'))()
"@ 10 "$Label did not open the mobile navigation menu."
}

function Close-MobileMenuWithScrim([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    [void](Invoke-Js $Socket "document.querySelector('[data-nav-scrim]')?.click(); true")
    Wait-ForJsCondition $Socket @"
(() => document.querySelector('[data-nav-toggle]')?.getAttribute('aria-expanded') === 'false'
  && !document.querySelector('[data-nav-panel]')?.classList.contains('is-open')
  && !document.body.classList.contains('nav-open'))()
"@ 10 "$Label did not close the mobile navigation menu."
}

function Assert-MobileMenuContents([System.Net.WebSockets.ClientWebSocket]$Socket, [string[]]$ExpectedLinks, [string[]]$ExpectedButtons, [string[]]$ForbiddenLinks, [string]$Label) {
    $state = Invoke-Js $Socket @"
(() => ({
  links: Array.from(document.querySelectorAll('[data-nav-panel] .site-nav__link')).map(link => link.textContent.trim()),
  buttons: Array.from(document.querySelectorAll('[data-nav-panel] .button')).map(button => button.textContent.trim())
}))()
"@

    foreach ($expectedLink in $ExpectedLinks) {
        Assert-True (@($state.links) -contains $expectedLink) "$Label mobile menu did not include the '$expectedLink' link."
    }

    foreach ($expectedButton in $ExpectedButtons) {
        Assert-True (@($state.buttons) -contains $expectedButton) "$Label mobile menu did not include the '$expectedButton' action."
    }

    foreach ($forbiddenLink in $ForbiddenLinks) {
        Assert-True (-not (@($state.links) -contains $forbiddenLink)) "$Label mobile menu still showed '$forbiddenLink' when it should not have."
    }
}

function Assert-DesktopNav([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Label) {
    $state = Invoke-Js $Socket @"
(() => {
  const toggle = document.querySelector('[data-nav-toggle]');
  const nav = document.querySelector('.site-nav');
  const toggleStyle = toggle ? window.getComputedStyle(toggle) : null;
  const navStyle = nav ? window.getComputedStyle(nav) : null;
  return {
    toggleVisible: !!toggle && toggleStyle.display !== 'none' && toggleStyle.visibility !== 'hidden',
    navVisible: !!nav && navStyle.display !== 'none' && navStyle.visibility !== 'hidden'
  };
})()
"@

    Assert-True ($state.toggleVisible -eq $false) "$Label still showed the hamburger toggle on the desktop viewport."
    Assert-True ($state.navVisible -eq $true) "$Label did not keep the full navbar visible on the desktop viewport."
}

function Login-ThroughUi([ref]$SocketRef, [int]$DebugPort, [string]$RootUrl, [string]$Email, [string]$Password, [string]$ExpectedPath, [string]$ReadyExpression, [string]$ReadyMessage, [string]$FailureLabel) {
    Navigate-To $SocketRef $DebugPort (Join-BaseUrl $RootUrl "/login.html")

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

function Register-ThroughUi([ref]$SocketRef, [int]$DebugPort, [string]$RootUrl, [string]$Name, [string]$Email, [string]$Password, [string]$ExpectedPath, [string]$ReadyExpression, [string]$ReadyMessage, [string]$FailureLabel) {
    Navigate-To $SocketRef $DebugPort (Join-BaseUrl $RootUrl "/register.html")

    $nameJson = $Name | ConvertTo-Json -Compress
    $emailJson = $Email | ConvertTo-Json -Compress
    $passwordJson = $Password | ConvertTo-Json -Compress
    [void](Invoke-Js $SocketRef.Value @"
(() => {
  document.getElementById('register-name').value = $nameJson;
  document.getElementById('register-email').value = $emailJson;
  document.getElementById('register-password').value = $passwordJson;
  document.getElementById('register-form').requestSubmit();
  return true;
})()
"@)

    $registrationCompletedOnCurrentSocket = $false
    try {
        Wait-ForJsCondition $SocketRef.Value "window.location.pathname.endsWith('$ExpectedPath')" 12 "$FailureLabel did not redirect to $ExpectedPath after registration."
        Wait-ForJsCondition $SocketRef.Value $ReadyExpression 25 $ReadyMessage
        $registrationCompletedOnCurrentSocket = $true
    }
    catch {
    }

    if (-not $registrationCompletedOnCurrentSocket) {
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
        } 25 "$FailureLabel did not redirect to $ExpectedPath after registration."
        Wait-ForJsCondition $SocketRef.Value $ReadyExpression 25 $ReadyMessage
    }
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
        } -ArgumentList $ProjectRoot, $ConnectionString, $normalizedBaseUrl
    }

    Wait-ForApp $normalizedBaseUrl
    $centerId = Get-FirstCenterId $normalizedBaseUrl

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

    Set-Viewport $webSocket 390 844 $true
    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/index.html")
    Wait-ForJsCondition $webSocket "document.querySelectorAll('#featured-centers article.card').length >= 1" 25 "The home page did not load featured centers on mobile."
    Assert-NoConsoleErrors $webSocket "The mobile home page"
    Assert-ResponsiveLayout $webSocket "The mobile home page"
    Assert-MobileNavVisible $webSocket "The mobile home page"
    Assert-TouchTargets $webSocket ".site-nav-toggle, .button" "The mobile home page"
    Open-MobileMenu $webSocket "The mobile home page"
    Assert-MobileMenuContents $webSocket @("Home", "Centers", "About", "Login", "Register") @() @("Dashboard") "The anonymous mobile home menu"
    Close-MobileMenuWithScrim $webSocket "The mobile home page"
    Open-MobileMenu $webSocket "The mobile home page"
    Click-AndWaitForPath ([ref]$webSocket) $debugPort @"
(() => {
  const link = Array.from(document.querySelectorAll('[data-nav-panel] .site-nav__link'))
    .find(item => item.textContent.trim() === 'Centers');
  if (!link) {
    throw new Error('The Centers link was not found in the mobile menu.');
  }

  link.click();
  return true;
})()
"@ "/centers.html" 20 "The mobile menu did not navigate to centers.html."
    Wait-ForJsCondition $webSocket "document.querySelectorAll('#centers-list article.card').length >= 1" 25 "The centers directory did not render on mobile."
    Assert-NoConsoleErrors $webSocket "The mobile centers page"
    Assert-ResponsiveLayout $webSocket "The mobile centers page"
    $mobileCentersState = Invoke-Js $webSocket @"
(() => ({
  menuClosed: document.querySelector('[data-nav-toggle]')?.getAttribute('aria-expanded') === 'false' && !document.body.classList.contains('nav-open'),
  activeLink: document.querySelector('.site-nav__link.is-active')?.textContent.trim() || ''
}))()
"@
    Assert-True ($mobileCentersState.menuClosed -eq $true) "The mobile menu stayed open after navigating to the centers page."
    Assert-True ($mobileCentersState.activeLink -eq "Centers") "The centers page did not mark the active navigation link after mobile navigation."

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/center-details.html?id=$centerId")
    Wait-ForJsCondition $webSocket "document.getElementById('center-detail-message')?.textContent.length > 0" 25 "The center details page did not finish loading on mobile."
    Assert-NoConsoleErrors $webSocket "The mobile center details page"
    Assert-ResponsiveLayout $webSocket "The mobile center details page"

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/login.html")
    Wait-ForJsCondition $webSocket "!!document.getElementById('login-form')" 20 "The login page did not initialize on mobile."
    Assert-NoConsoleErrors $webSocket "The mobile login page"
    Assert-ResponsiveLayout $webSocket "The mobile login page"
    Assert-FormsFitViewport $webSocket "The mobile login page"
    Assert-TouchTargets $webSocket "input, button, .site-nav-toggle" "The mobile login page"

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/register.html")
    Wait-ForJsCondition $webSocket "!!document.getElementById('register-form')" 20 "The register page did not initialize on mobile."
    Assert-NoConsoleErrors $webSocket "The mobile register page"
    Assert-ResponsiveLayout $webSocket "The mobile register page"
    Assert-FormsFitViewport $webSocket "The mobile register page"
    Assert-TouchTargets $webSocket "input, button, select, textarea, .site-nav-toggle" "The mobile register page"

    Register-ThroughUi ([ref]$webSocket) $debugPort $normalizedBaseUrl "Mobile Smoke User $tempUserSuffix" $tempUserEmail $tempUserPassword "/dashboard.html" "document.body.dataset.authReady === 'true' && document.getElementById('dashboard-page-message')?.textContent.includes('Your contribution tools are ready')" "The user dashboard did not finish loading after registration." "The mobile user flow"
    Set-Viewport $webSocket 390 844 $true
    Assert-NoConsoleErrors $webSocket "The mobile user dashboard"
    Assert-ResponsiveLayout $webSocket "The mobile user dashboard"
    Assert-FormsFitViewport $webSocket "The mobile user dashboard"
    Assert-TouchTargets $webSocket ".button, .dashboard-nav__link, .site-nav-toggle, input, textarea, select" "The mobile user dashboard"

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/profile.html")
    Wait-ForJsCondition $webSocket "document.body.dataset.authReady === 'true' && !!document.getElementById('profile-form')" 20 "The profile page did not initialize on mobile."
    Assert-NoConsoleErrors $webSocket "The mobile profile page"
    Assert-ResponsiveLayout $webSocket "The mobile profile page"
    Assert-FormsFitViewport $webSocket "The mobile profile page"
    Assert-TouchTargets $webSocket "input, button, .site-nav-toggle" "The mobile profile page"

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/dashboard.html")
    Wait-ForJsCondition $webSocket "document.body.dataset.authReady === 'true' && document.getElementById('dashboard-page-message')?.textContent.length > 0" 20 "The user dashboard did not return after visiting the profile page."
    Open-MobileMenu $webSocket "The mobile user dashboard"
    Assert-MobileMenuContents $webSocket @("Home", "Centers", "About", "Dashboard", "Profile") @("My profile", "Logout") @("Login", "Register") "The authenticated mobile dashboard menu"
    $dashboardMenuState = Invoke-Js $webSocket "document.querySelector('[data-nav-panel] .site-nav__link.is-active')?.textContent.trim() || ''"
    Assert-True ($dashboardMenuState -eq "Dashboard") "The mobile dashboard menu did not show Dashboard as the active link."
    Click-AndWaitForPath ([ref]$webSocket) $debugPort @"
(() => {
  const button = document.querySelector('[data-nav-panel] [data-logout-action]');
  if (!button) {
    throw new Error('The mobile logout action was not found.');
  }

  button.click();
  return true;
})()
"@ "/login.html" 25 "The mobile logout flow did not return to login.html."
    Assert-NoConsoleErrors $webSocket "The post-logout mobile login page"
    Wait-ForJsCondition $webSocket @"
(() => !localStorage.getItem('noorlocator.auth.token')
  && !localStorage.getItem('noorlocator.auth.refreshToken')
  && !localStorage.getItem('noorlocator.auth.user')
  && !sessionStorage.getItem('noorlocator.auth.token')
  && !sessionStorage.getItem('noorlocator.auth.refreshToken')
  && !sessionStorage.getItem('noorlocator.auth.user'))()
"@ 10 "The mobile logout flow did not clear the stored authentication session."
    Open-MobileMenu $webSocket "The post-logout mobile login page"
    Assert-MobileMenuContents $webSocket @("Home", "Centers", "About", "Login", "Register") @() @("Dashboard", "Profile") "The logged-out mobile menu"
    Close-MobileMenuWithScrim $webSocket "The post-logout mobile login page"

    Login-ThroughUi ([ref]$webSocket) $debugPort $normalizedBaseUrl $ManagerEmail $ManagerPassword "/manager.html" "document.body.dataset.authReady === 'true' && document.getElementById('manager-page-message')?.textContent.includes('Manager workspace loaded from the live API')" "The manager workspace did not finish loading after login." "The mobile manager flow"
    Set-Viewport $webSocket 390 844 $true
    Assert-NoConsoleErrors $webSocket "The mobile manager dashboard"
    Assert-ResponsiveLayout $webSocket "The mobile manager dashboard"
    Assert-TouchTargets $webSocket ".button, .dashboard-nav__link, .site-nav-toggle, .form__control, .checkbox, .checkbox-card" "The mobile manager dashboard"
    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/logout.html")
    Wait-ForJsCondition $webSocket "window.location.pathname.endsWith('/login.html')" 20 "The manager logout flow did not return to login.html."

    Login-ThroughUi ([ref]$webSocket) $debugPort $normalizedBaseUrl $AdminEmail $AdminPassword "/admin.html" "document.body.dataset.authReady === 'true' && !!document.getElementById('admin-user-search') && !document.body.innerText.includes('window.NoorLocatorApi.getAdminManagerAssignments is not a function') && !document.body.innerText.includes('outdated files')" "The admin workspace did not finish loading after login." "The mobile admin flow"
    Set-Viewport $webSocket 390 844 $true
    Assert-NoConsoleErrors $webSocket "The mobile admin dashboard"
    Assert-ResponsiveLayout $webSocket "The mobile admin dashboard"
    Assert-TouchTargets $webSocket ".button, .dashboard-nav__link, .site-nav-toggle" "The mobile admin dashboard"
    $adminTableState = Invoke-Js $webSocket @"
(() => ({
  hasStackedLabels: Array.from(document.querySelectorAll('.data-table td')).some(cell => cell.hasAttribute('data-label')),
  hasTables: document.querySelectorAll('.data-table').length >= 1
}))()
"@
    Assert-True ($adminTableState.hasTables -eq $true) "The mobile admin dashboard did not render the admin data tables."
    Assert-True ($adminTableState.hasStackedLabels -eq $true) "The mobile admin dashboard tables are missing stacked mobile labels."
    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/logout.html")
    Wait-ForJsCondition $webSocket "window.location.pathname.endsWith('/login.html')" 20 "The admin logout flow did not return to login.html."

    Set-Viewport $webSocket 820 1180 $true
    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/index.html")
    Wait-ForJsCondition $webSocket "document.querySelectorAll('#featured-centers article.card').length >= 1" 25 "The tablet home page did not load featured centers."
    Assert-NoConsoleErrors $webSocket "The tablet home page"
    Assert-ResponsiveLayout $webSocket "The tablet home page"

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/center-details.html?id=$centerId")
    Wait-ForJsCondition $webSocket "document.getElementById('center-detail-message')?.textContent.length > 0" 25 "The tablet center details page did not finish loading."
    Assert-NoConsoleErrors $webSocket "The tablet center details page"
    Assert-ResponsiveLayout $webSocket "The tablet center details page"

    Set-Viewport $webSocket 1440 900 $false
    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/index.html")
    Wait-ForJsCondition $webSocket "document.querySelectorAll('#featured-centers article.card').length >= 1" 25 "The desktop home page did not load featured centers."
    Assert-NoConsoleErrors $webSocket "The desktop home page"
    Assert-ResponsiveLayout $webSocket "The desktop home page"
    Assert-DesktopNav $webSocket "The desktop home page"

    Navigate-To ([ref]$webSocket) $debugPort (Join-BaseUrl $normalizedBaseUrl "/centers.html")
    Wait-ForJsCondition $webSocket "document.querySelectorAll('#centers-list article.card').length >= 1" 25 "The desktop centers page did not render public centers."
    Assert-NoConsoleErrors $webSocket "The desktop centers page"
    Assert-ResponsiveLayout $webSocket "The desktop centers page"

    $summary.MobileResponsive = "Verified home, centers, center details, login, register, dashboard, manager, and admin pages at a mobile viewport without console errors or horizontal overflow."
    $summary.MobileNavbar = "Verified the hamburger menu appears on mobile, opens and closes cleanly, closes after outside interaction and route selection, keeps the active link visible, and updates correctly across login and logout."
    $summary.TouchUx = "Verified visible touch targets meet the 44x44 guideline on key routes and mobile forms stay within the viewport."
    $summary.CapacitorReadiness = "Verified relative public routes, relative manifest start URL, service worker registration guard for non-http(s) protocols, and responsive pages under mobile browser emulation."
    $summary.TabletDesktop = "Verified representative tablet and desktop layouts load without overflow, and desktop keeps the full navbar visible without the mobile toggle."

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

    if ($appJob) {
        try {
            Stop-Job -Job $appJob -ErrorAction SilentlyContinue | Out-Null
            Remove-Job -Job $appJob -Force -ErrorAction SilentlyContinue | Out-Null
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
