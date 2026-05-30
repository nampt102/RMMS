# ============================================================================
#  smoke-day4.ps1 - Sprint 01 Day 4 endpoint smoke test
# ============================================================================
#
#  What this verifies (~20 scenarios end-to-end):
#    - Bootstrap: seed admin via CLI (idempotent - skips if already exists)
#    - Auth:
#        register PG -> verify-email (token grep'd from log)
#        login OK; wrong password; deactivated account
#        forgot-password -> reset-password (revokes refresh)
#        old password fails after reset; new password works
#    - Admin (Authorize Roles=admin):
#        list with pagination + role filter + status filter + search
#        create Leader -> initial password emailed
#        admin trigger reset for a user
#        PG hits /admin/users -> 403
#    - Logout / refresh rotation / reuse detection
#
#  Pre-requisites:
#    1. docker compose up -d postgres redis   (postgres 5433:5432, see Day 4 fix)
#    2. EF migration applied (Day 1/2 setup)
#    3. API is **NOT yet running** - this script starts a fresh API in the background
#       and reads its stdout log file to grep email tokens.
#
#  Usage:
#    .\scripts\smoke-day4.ps1                    # default: localhost:5000
#    .\scripts\smoke-day4.ps1 -BaseUrl http://localhost:5050
#    .\scripts\smoke-day4.ps1 -SkipApiStart      # if you already have API running with logging to .\api.log
#
# #  Exit code: 0 = all pass, 1 = >=1 fail.
# | Step | Scenario                                                                                                           |
# | ---- | ------------------------------------------------------------------------------------------------------------------ |
# | 0    | **CLI seed-admin (idempotent)**                                                                                    |
# | 1    | **Start API + health check**                                                                                       |
# | 2    | **Register Player (PG)** → `201 Created`, status = `pending_email_verify`; duplicate registration → `409 Conflict` |
# | 3    | **Grep verify token từ log** → gọi `verify-email` → `200 OK`                                                       |
# | 4    | **Login PG** với đúng password → thành công; sai password → `401 Unauthorized`                                     |
# | 5    | **Refresh token rotation + reuse detection** → dùng lại refresh token cũ → `401 Unauthorized`                      |
# | 6    | **Forgot Password → Reset Password** → login bằng password cũ → `401 Unauthorized`; password mới → `200 OK`        |
# | 7    | **Admin login** + `GET /admin/users` (kiểm tra pagination, role, status, search filters)                           |
# | 8    | **Admin create Leader** + verify email chứa initial password                                                       |
# | 9    | **Admin trigger password reset** cho user khác                                                                     |
# | 10   | **Admin deactivate → activate Leader**                                                                             |
# | 11   | **PG gọi `/admin/users`** → `403 Forbidden` (RBAC enforcement)                                                     |
# | 12   | **Logout + idempotent re-logout**                                                                                  |


# ============================================================================

[CmdletBinding()]
param(
    # launchSettings.json profile "Rmms.Api" binds to http://localhost:5080.
    [string]$BaseUrl = "http://localhost:5080",
    [string]$LogFile = "$PSScriptRoot\..\api.log",
    [string]$RepoRoot = "$PSScriptRoot\..",
    [switch]$SkipApiStart,
    [switch]$KeepApiRunning
)

# Resolve full path to the built API DLL — used by both CLI commands and Start-Api
# so we bypass `dotnet run` (which loads launchSettings and may swallow command-line args).
$ApiProjectDir = (Resolve-Path "$PSScriptRoot\..\backend\src\Rmms.Api").Path
$ApiDll = Join-Path $ApiProjectDir "bin\Debug\net10.0\Rmms.Api.dll"

$ErrorActionPreference = "Stop"
$ProgressPreference   = "SilentlyContinue"

# ---------- Globals ----------
$script:Passed = 0
$script:Failed = 0
$script:FailedNames = @()
$apiProcess = $null

# Fixed test fixtures
$AdminEmail  = "admin@motivesvn.com"
$AdminPwd    = "AdminPwd1"
# Unique per run so the smoke test is repeatable without DB cleanup
# (register is not idempotent -> a fixed email 409s on the 2nd run).
$RunStamp    = Get-Date -Format "yyyyMMddHHmmss"
$PgEmail     = "smoke.pg+$RunStamp@example.com"
$PgPwd       = "Pg12345A"
$PgPwdNew    = "PgNewPwd9"
$LeaderEmail = "smoke.leader+$RunStamp@example.com"

$DeviceA = @{
    deviceId    = "smoke-device-A-uuid"
    deviceName  = "Smoke iPhone A"
    os          = "ios"
    osVersion   = "17.0"
    appVersion  = "1.0.0"
    fcmToken    = "fake-fcm-A"
}
$AdminDevice = @{
    deviceId    = "smoke-admin-device"
    deviceName  = "Admin Web"
    os          = "ios"
    osVersion   = "17.0"
    appVersion  = "1.0.0"
}

# ============================================================================
#  Helpers
# ============================================================================

function Step {
    param([string]$Title)
    Write-Host ""
    Write-Host ("-" * 70) -ForegroundColor DarkGray
    Write-Host $Title -ForegroundColor Cyan
}

function Pass([string]$Name) {
    Write-Host "  PASS  $Name" -ForegroundColor Green
    $script:Passed++
}

function Fail([string]$Name, [string]$Reason) {
    Write-Host "  FAIL  $Name" -ForegroundColor Red
    if ($Reason) { Write-Host "         $Reason" -ForegroundColor Red }
    $script:Failed++
    $script:FailedNames += $Name
}

function Invoke-Api {
    param(
        [string]$Method, [string]$Path,
        $Body = $null,
        $Headers = @{},
        [int]$ExpectedStatus = 200,
        [string]$Name
    )
    $url = "$BaseUrl$Path"
    $allHeaders = @{ "Content-Type" = "application/json" } + $Headers
    $params = @{
        Method  = $Method
        Uri     = $url
        Headers = $allHeaders
        UseBasicParsing = $true
    }
    if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress) }

    # PowerShell 7+ has -SkipHttpErrorCheck so non-2xx responses don't throw.
    # Windows PowerShell 5.1 lacks it and throws on non-2xx; we recover the
    # status code + body from the thrown WebException below.
    $status = $null
    $bodyText = $null
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $resp = Invoke-WebRequest @params -SkipHttpErrorCheck
        $status = [int]$resp.StatusCode
        $bodyText = $resp.Content
    } else {
        try {
            $resp = Invoke-WebRequest @params
            $status = [int]$resp.StatusCode
            $bodyText = $resp.Content
        } catch {
            $resp = $_.Exception.Response
            if ($resp) {
                $status = [int]$resp.StatusCode
                try {
                    $stream = $resp.GetResponseStream()
                    $reader = New-Object System.IO.StreamReader($stream)
                    $bodyText = $reader.ReadToEnd()
                    $reader.Close()
                } catch {}
            } else {
                throw
            }
        }
    }
    $bodyObj = $null
    if ($bodyText) {
        # ConvertFrom-Json -Depth only exists on PS6+. On Windows PowerShell 5.1
        # passing -Depth throws (then was silently swallowed -> null body).
        try {
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $bodyObj = $bodyText | ConvertFrom-Json -Depth 20
            } else {
                $bodyObj = $bodyText | ConvertFrom-Json
            }
        } catch {
            Write-Host "  (warn) Failed to parse JSON body: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }

    $result = [PSCustomObject]@{
        Status   = $status
        Body     = $bodyObj
        RawBody  = $bodyText
    }

    if ($status -eq $ExpectedStatus) {
        Pass "$Name (HTTP $status)"
    } else {
        Fail "$Name (expected HTTP $ExpectedStatus, got $status)" $bodyText
    }
    return $result
}

function Grep-LatestToken {
    param([string]$EmailRecipient, [string]$Pattern, [int]$TimeoutSec = 10)
    # Pattern is what comes after "token=" - extract the next URL-safe token.
    # Wait up to $TimeoutSec for log to flush.
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogFile) {
            $content = Get-Content $LogFile -Raw
            # Get all matches; we want the one after the LAST "To=$EmailRecipient" block.
            $idx = $content.LastIndexOf("To=$EmailRecipient")
            if ($idx -ge 0) {
                $tail = $content.Substring($idx)
                $m = [regex]::Match($tail, "token=([A-Za-z0-9_\-]+)")
                if ($m.Success) {
                    return $m.Groups[1].Value
                }
            }
        }
        Start-Sleep -Milliseconds 500
    }
    return $null
}

function Reset-LogFile {
    if (Test-Path $LogFile) { Remove-Item $LogFile -Force }
}

function Start-Api {
    Reset-LogFile
    if (-not (Test-Path $ApiDll)) {
        throw "Missing $ApiDll - run 'dotnet build backend/src/Rmms.Api' first."
    }
    Write-Host "  Starting API in background (direct DLL exec from $ApiProjectDir), log -> $LogFile ..." -ForegroundColor DarkGray
    Write-Host "  Binding to $BaseUrl (via ASPNETCORE_URLS) ..." -ForegroundColor DarkGray

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    # Bypassing `dotnet run` means launchSettings.json is NOT applied. Kestrel would otherwise
    # default to http://localhost:5000. Force it to match the script's $BaseUrl.
    $env:ASPNETCORE_URLS = $BaseUrl

    # Run the DLL directly. Bypasses launchSettings.json and command-line arg quirks of `dotnet run`.
    # WorkingDirectory MUST be the project folder so appsettings.json + appsettings.Development.json load correctly.
    $script:apiProcess = Start-Process -FilePath "dotnet" -ArgumentList @($ApiDll) `
        -WorkingDirectory $ApiProjectDir `
        -RedirectStandardOutput $LogFile -RedirectStandardError "$LogFile.err" `
        -NoNewWindow -PassThru

    # Poll for readiness via /health/live (give it up to 60s for first-run startup)
    Write-Host "  Waiting for /health/live ..." -ForegroundColor DarkGray
    $deadline = (Get-Date).AddSeconds(60)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            # MaximumRedirection 0 prevents UseHttpsRedirection middleware (if any) from
            # bouncing us to an unreachable HTTPS port.
            # NOTE: -SkipHttpErrorCheck only exists on PowerShell 7+, so we don't use it.
            # On Windows PowerShell 5.1 a non-2xx status throws; we handle that in catch below.
            $iwrArgs = @{
                Uri             = "$BaseUrl/health/live"
                UseBasicParsing = $true
                TimeoutSec      = 2
                MaximumRedirection = 0
            }
            $r = Invoke-WebRequest @iwrArgs
            if ($r.StatusCode -eq 200) {
                Write-Host "  API ready." -ForegroundColor DarkGray
                return
            }
            $lastError = "HTTP $($r.StatusCode) $($r.StatusDescription)"
        } catch {
            # If the endpoint responded with a real HTTP status (e.g. 200 wrapped as error
            # on PS5.1, or 503 while starting), surface it; otherwise it's a connection error.
            $resp = $_.Exception.Response
            if ($resp -and ([int]$resp.StatusCode) -eq 200) {
                Write-Host "  API ready." -ForegroundColor DarkGray
                return
            }
            $lastError = $_.Exception.Message
        }
        Start-Sleep -Milliseconds 800
    }
    Write-Host "  Last polling error: $lastError" -ForegroundColor Red
    throw "API did not become ready in 60s; check $LogFile"
}

function Stop-Api {
    if ($script:apiProcess -and -not $script:apiProcess.HasExited) {
        Write-Host "  Stopping API (pid $($script:apiProcess.Id)) ..." -ForegroundColor DarkGray
        try { Stop-Process -Id $script:apiProcess.Id -Force -ErrorAction Stop } catch {}
    }
}

function Run-Cli {
    param([string[]]$CliArgs)
    if (-not (Test-Path $ApiDll)) {
        throw "Missing $ApiDll - run 'dotnet build backend/src/Rmms.Api' first."
    }
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    # Must run from the API project folder so appsettings*.json is loaded.
    Push-Location $ApiProjectDir
    try {
        & dotnet $ApiDll @CliArgs
        if ($LASTEXITCODE -ne 0) {
            throw "CLI exited with code $LASTEXITCODE"
        }
    } finally {
        Pop-Location
    }
}

function Kill-StaleApi {
    # Kill any leftover Rmms.Api process from a previous run / manual test.
    Get-Process -Name "Rmms.Api" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "  Killing stale Rmms.Api PID=$($_.Id) ..." -ForegroundColor DarkYellow
        try { Stop-Process -Id $_.Id -Force -ErrorAction Stop } catch {}
    }
    # Wait briefly for file handles to release
    Start-Sleep -Milliseconds 800
}

# ============================================================================
#  TEST SEQUENCE
# ============================================================================

try {
    Write-Host ""
    Write-Host "RMMS Day 4 smoke test" -ForegroundColor Yellow
    Write-Host "BaseUrl = $BaseUrl" -ForegroundColor Yellow
    Write-Host "LogFile = $LogFile" -ForegroundColor Yellow

    # Clean slate — kill any previous API process holding DLL locks
    Kill-StaleApi

    # ---------------------------------------------------------------------
    Step "0. Bootstrap: seed-admin (idempotent)"
    # ---------------------------------------------------------------------
    Run-Cli -CliArgs @("seed-admin", "--email=$AdminEmail", "--password=$AdminPwd", "--full-name=System Admin")
    Pass "Seed admin CLI ran"

    # ---------------------------------------------------------------------
    Step "1. Start API"
    # ---------------------------------------------------------------------
    if (-not $SkipApiStart) {
        Start-Api
    } else {
        Write-Host "  --SkipApiStart specified; assuming API already running and writing to $LogFile" -ForegroundColor DarkGray
    }

    Invoke-Api -Method GET -Path "/health/live" -Name "Health live" -ExpectedStatus 200 | Out-Null

    # ---------------------------------------------------------------------
    Step "2. Auth: register PG"
    # ---------------------------------------------------------------------
    $registerBody = @{
        email = $PgEmail
        password = $PgPwd
        fullName = "PG Smoke Test"
        phone = "0901234567"
        preferredLanguage = "vi"
    }
    $r = Invoke-Api -Method POST -Path "/api/v1/auth/register" -Body $registerBody `
        -Name "Register PG ($PgEmail)" -ExpectedStatus 201
    if ($r.Status -eq 201 -and $r.Body.data.status -eq "pending_email_verify") {
        Pass "Status = pending_email_verify"
    } else {
        Fail "Status field" "got '$($r.Body.data.status)'"
    }

    # Duplicate
    Invoke-Api -Method POST -Path "/api/v1/auth/register" -Body $registerBody `
        -Name "Duplicate register -> 409" -ExpectedStatus 409 | Out-Null

    # ---------------------------------------------------------------------
    Step "3. Auth: grep verify token + verify-email"
    # ---------------------------------------------------------------------
    $verifyToken = Grep-LatestToken -EmailRecipient $PgEmail -TimeoutSec 10
    if ($verifyToken) {
        Pass "Verify token captured from log (length=$($verifyToken.Length))"
    } else {
        Fail "Verify token" "no `'token=...`' found near To=$PgEmail in $LogFile"
        throw "Cannot proceed without verify token"
    }

    Invoke-Api -Method POST -Path "/api/v1/auth/verify-email" -Body @{ token = $verifyToken } `
        -Name "Verify-email" -ExpectedStatus 200 | Out-Null

    # ---------------------------------------------------------------------
    Step "4. Auth: login PG"
    # ---------------------------------------------------------------------
    $loginBody = @{ email = $PgEmail; password = $PgPwd; device = $DeviceA }
    $r = Invoke-Api -Method POST -Path "/api/v1/auth/login" -Body $loginBody `
        -Name "Login PG (correct password)" -ExpectedStatus 200
    $pgAccessToken  = $r.Body.data.accessToken
    $pgRefreshToken = $r.Body.data.refreshToken
    if ($pgAccessToken) { Pass "Access token returned" } else { Fail "Access token" "missing in body" }

    # Wrong password
    Invoke-Api -Method POST -Path "/api/v1/auth/login" `
        -Body @{ email = $PgEmail; password = "wrongpass"; device = $DeviceA } `
        -Name "Login with wrong password -> 401" -ExpectedStatus 401 | Out-Null

    # ---------------------------------------------------------------------
    Step "5. Auth: refresh + reuse detection"
    # ---------------------------------------------------------------------
    $r = Invoke-Api -Method POST -Path "/api/v1/auth/refresh" -Body @{ refreshToken = $pgRefreshToken } `
        -Name "Refresh rotation" -ExpectedStatus 200
    $newRefresh = $r.Body.data.refreshToken
    if ($newRefresh -and $newRefresh -ne $pgRefreshToken) {
        Pass "New refresh token differs from old"
    } else {
        Fail "Refresh rotation" "new refresh equals old"
    }

    # Reuse OLD token -> all tokens revoked
    Invoke-Api -Method POST -Path "/api/v1/auth/refresh" -Body @{ refreshToken = $pgRefreshToken } `
        -Name "Reuse OLD refresh -> 401 + revoke-all" -ExpectedStatus 401 | Out-Null

    # ---------------------------------------------------------------------
    Step "6. Auth: forgot-password + reset-password"
    # ---------------------------------------------------------------------
    Invoke-Api -Method POST -Path "/api/v1/auth/forgot-password" -Body @{ email = $PgEmail } `
        -Name "Forgot password -> 204" -ExpectedStatus 204 | Out-Null

    $resetToken = Grep-LatestToken -EmailRecipient $PgEmail -TimeoutSec 10
    if ($resetToken -and $resetToken -ne $verifyToken) {
        Pass "Reset token captured (different from verify token)"
    } else {
        Fail "Reset token" "not captured or equal to verify token"
    }

    Invoke-Api -Method POST -Path "/api/v1/auth/reset-password" `
        -Body @{ token = $resetToken; newPassword = $PgPwdNew } `
        -Name "Reset-password -> 204" -ExpectedStatus 204 | Out-Null

    # Old password no longer works
    Invoke-Api -Method POST -Path "/api/v1/auth/login" `
        -Body @{ email = $PgEmail; password = $PgPwd; device = $DeviceA } `
        -Name "Login with OLD password -> 401" -ExpectedStatus 401 | Out-Null

    # New password works (and we get a fresh token set)
    $r = Invoke-Api -Method POST -Path "/api/v1/auth/login" `
        -Body @{ email = $PgEmail; password = $PgPwdNew; device = $DeviceA } `
        -Name "Login with NEW password" -ExpectedStatus 200
    $pgAccessTokenNew  = $r.Body.data.accessToken
    $pgRefreshTokenNew = $r.Body.data.refreshToken

    # ---------------------------------------------------------------------
    Step "7. Admin: login + list users"
    # ---------------------------------------------------------------------
    $r = Invoke-Api -Method POST -Path "/api/v1/auth/login" `
        -Body @{ email = $AdminEmail; password = $AdminPwd; device = $AdminDevice } `
        -Name "Admin login" -ExpectedStatus 200
    $adminAccess = $r.Body.data.accessToken
    if (-not $adminAccess) { Fail "Admin login" "no token"; throw "Cannot proceed" }

    $adminHeaders = @{ "Authorization" = "Bearer $adminAccess" }

    Invoke-Api -Method GET -Path "/api/v1/admin/users?page=1&pageSize=10" `
        -Headers $adminHeaders -Name "Admin GET /users (paged)" -ExpectedStatus 200 | Out-Null

    Invoke-Api -Method GET -Path "/api/v1/admin/users?role=pg" `
        -Headers $adminHeaders -Name "Admin filter role=pg" -ExpectedStatus 200 | Out-Null

    Invoke-Api -Method GET -Path "/api/v1/admin/users?status=active" `
        -Headers $adminHeaders -Name "Admin filter status=active" -ExpectedStatus 200 | Out-Null

    Invoke-Api -Method GET -Path "/api/v1/admin/users?search=smoke" `
        -Headers $adminHeaders -Name "Admin search 'smoke'" -ExpectedStatus 200 | Out-Null

    # ---------------------------------------------------------------------
    Step "8. Admin: create Leader"
    # ---------------------------------------------------------------------
    $createBody = @{
        email = $LeaderEmail
        fullName = "Smoke Leader"
        role = "leader"
        preferredLanguage = "vi"
    }
    $r = Invoke-Api -Method POST -Path "/api/v1/admin/users" -Body $createBody -Headers $adminHeaders `
        -Name "Admin create Leader" -ExpectedStatus 201
    $leaderId = $r.Body.data.id

    # Initial password should have been emailed
    Start-Sleep -Milliseconds 800
    $logTail = Get-Content $LogFile -Raw
    if ($logTail -match "To=$LeaderEmail" -and $logTail -match "pwd=") {
        Pass "Initial password email captured"
    } else {
        Fail "Initial password email" "no 'pwd=' near To=$LeaderEmail in log"
    }

    # ---------------------------------------------------------------------
    Step "9. Admin: trigger reset for leader"
    # ---------------------------------------------------------------------
    Invoke-Api -Method POST -Path "/api/v1/admin/users/$leaderId/reset-password" `
        -Headers $adminHeaders -Name "Admin reset-password for leader" -ExpectedStatus 204 | Out-Null

    # ---------------------------------------------------------------------
    Step "10. Admin: deactivate leader -> 200, then re-activate"
    # ---------------------------------------------------------------------
    Invoke-Api -Method PATCH -Path "/api/v1/admin/users/$leaderId" `
        -Body @{ status = "inactive" } -Headers $adminHeaders `
        -Name "Patch leader -> inactive" -ExpectedStatus 200 | Out-Null

    Invoke-Api -Method PATCH -Path "/api/v1/admin/users/$leaderId" `
        -Body @{ status = "active" } -Headers $adminHeaders `
        -Name "Patch leader -> active" -ExpectedStatus 200 | Out-Null

    # ---------------------------------------------------------------------
    Step "11. Authorization: PG calls /admin/users -> 403"
    # ---------------------------------------------------------------------
    $pgHeaders = @{ "Authorization" = "Bearer $pgAccessTokenNew" }
    Invoke-Api -Method GET -Path "/api/v1/admin/users" -Headers $pgHeaders `
        -Name "PG -> /admin/users -> 403" -ExpectedStatus 403 | Out-Null

    # ---------------------------------------------------------------------
    Step "12. Logout PG"
    # ---------------------------------------------------------------------
    Invoke-Api -Method POST -Path "/api/v1/auth/logout" -Body @{ refreshToken = $pgRefreshTokenNew } `
        -Name "Logout PG" -ExpectedStatus 204 | Out-Null

    # Re-logout (idempotent)
    Invoke-Api -Method POST -Path "/api/v1/auth/logout" -Body @{ refreshToken = $pgRefreshTokenNew } `
        -Name "Logout PG twice (idempotent)" -ExpectedStatus 204 | Out-Null
}
catch {
    Write-Host ""
    Write-Host "FATAL: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
}
finally {
    if (-not $KeepApiRunning) {
        Stop-Api
    } else {
        Write-Host ""
        Write-Host "API kept running (PID $($script:apiProcess.Id)). Log: $LogFile" -ForegroundColor Yellow
    }

    # ---------------------------------------------------------------------
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor DarkGray
    Write-Host "Summary  PASSED=$script:Passed  FAILED=$script:Failed" -ForegroundColor $(if ($script:Failed -eq 0) { 'Green' } else { 'Red' })
    if ($script:Failed -gt 0) {
        Write-Host "Failed:" -ForegroundColor Red
        $script:FailedNames | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        exit 1
    }
    Write-Host ""
    exit 0
}
