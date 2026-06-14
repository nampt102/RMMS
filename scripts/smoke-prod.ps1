# ============================================================================
#  smoke-prod.ps1 - Phase 1A production smoke test (READ-ONLY, prod-safe)
# ============================================================================
#
#  Verifies a DEPLOYED RMMS API is healthy and the Phase 1A surface answers.
#  All checks are READ-ONLY (one admin login + GET requests) so it is safe to
#  run against production. It does NOT seed, start the API, or mutate data.
#
#  What it checks:
#    - Liveness / readiness probes
#    - Admin login (non-PG, device-less per BR-105) -> JWT
#    - /auth/me identity round-trip
#    - One representative GET per shipped Phase 1A module:
#        M15 dashboard, M12 monitoring, M01 users, M05/M16 attendance,
#        M09 approvals, M16 audit, M03 stores
#    - RBAC negative: admin endpoint WITHOUT token -> 401
#
#  Usage:
#    $env:RMMS_SMOKE_ADMIN_PWD = "..."        # preferred: keep pwd out of history
#    .\scripts\smoke-prod.ps1 -BaseUrl https://rmms.example.com
#    .\scripts\smoke-prod.ps1 -BaseUrl http://103.216.116.206:5080 -AdminEmail admin@motivesvn.com -AdminPwd '...'
#
#  Exit code: 0 = all pass, 1 = >=1 fail.
# ============================================================================

[CmdletBinding()]
param(
    [string]$BaseUrl    = "http://localhost:5080",
    [string]$AdminEmail = "admin@motivesvn.com",
    # Prefer the env var RMMS_SMOKE_ADMIN_PWD; -AdminPwd overrides it.
    [string]$AdminPwd   = $env:RMMS_SMOKE_ADMIN_PWD
)

$ErrorActionPreference = "Stop"
$ProgressPreference    = "SilentlyContinue"

$script:Passed = 0
$script:Failed = 0
$script:FailedNames = @()

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

# PS5.1 + PS7-compatible HTTP helper (mirrors scripts/smoke-day4.ps1).
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
    $params = @{ Method = $Method; Uri = $url; Headers = $allHeaders; UseBasicParsing = $true; TimeoutSec = 15 }
    if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress) }

    $status = $null; $bodyText = $null
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $resp = Invoke-WebRequest @params -SkipHttpErrorCheck
        $status = [int]$resp.StatusCode; $bodyText = $resp.Content
    } else {
        try {
            $resp = Invoke-WebRequest @params
            $status = [int]$resp.StatusCode; $bodyText = $resp.Content
        } catch {
            $resp = $_.Exception.Response
            if ($resp) {
                $status = [int]$resp.StatusCode
                try {
                    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
                    $bodyText = $reader.ReadToEnd(); $reader.Close()
                } catch {}
            } else { throw }
        }
    }
    $bodyObj = $null
    if ($bodyText) {
        try {
            if ($PSVersionTable.PSVersion.Major -ge 6) { $bodyObj = $bodyText | ConvertFrom-Json -Depth 20 }
            else { $bodyObj = $bodyText | ConvertFrom-Json }
        } catch {}
    }
    $result = [PSCustomObject]@{ Status = $status; Body = $bodyObj; RawBody = $bodyText }
    if ($status -eq $ExpectedStatus) { Pass "$Name (HTTP $status)" }
    else { Fail "$Name (expected $ExpectedStatus, got $status)" $bodyText }
    return $result
}

try {
    Write-Host ""
    Write-Host "RMMS Phase 1A production smoke test" -ForegroundColor Yellow
    Write-Host "BaseUrl = $BaseUrl" -ForegroundColor Yellow
    Write-Host "Admin   = $AdminEmail" -ForegroundColor Yellow

    if (-not $AdminPwd) {
        throw "Admin password not provided. Set `$env:RMMS_SMOKE_ADMIN_PWD or pass -AdminPwd."
    }

    Write-Host "`n-- Health --" -ForegroundColor Cyan
    Invoke-Api -Method GET -Path "/health/live"  -Name "Health live"  -ExpectedStatus 200 | Out-Null
    Invoke-Api -Method GET -Path "/health/ready" -Name "Health ready" -ExpectedStatus 200 | Out-Null

    Write-Host "`n-- Auth --" -ForegroundColor Cyan
    # Non-PG login is device-less (BR-105 is PG-scoped).
    $r = Invoke-Api -Method POST -Path "/api/v1/auth/login" `
        -Body @{ email = $AdminEmail; password = $AdminPwd } `
        -Name "Admin login" -ExpectedStatus 200
    $adminAccess = $r.Body.data.accessToken
    if (-not $adminAccess) { Fail "Admin token" "no accessToken in body"; throw "Cannot proceed without token" }
    Pass "Access token returned"
    $h = @{ "Authorization" = "Bearer $adminAccess" }

    Invoke-Api -Method GET -Path "/api/v1/auth/me" -Headers $h -Name "GET /auth/me" -ExpectedStatus 200 | Out-Null

    Write-Host "`n-- Phase 1A read surface --" -ForegroundColor Cyan
    Invoke-Api -Method GET -Path "/api/v1/admin/dashboard/summary"     -Headers $h -Name "M15 dashboard summary"  | Out-Null
    Invoke-Api -Method GET -Path "/api/v1/team-monitoring/today"       -Headers $h -Name "M12 monitoring today"   | Out-Null
    Invoke-Api -Method GET -Path "/api/v1/admin/users?page=1&pageSize=5" -Headers $h -Name "M01 users (paged)"     | Out-Null
    Invoke-Api -Method GET -Path "/api/v1/admin/attendance?page=1&pageSize=5" -Headers $h -Name "M05/M16 attendance list" | Out-Null
    Invoke-Api -Method GET -Path "/api/v1/admin/approvals?page=1&pageSize=5"  -Headers $h -Name "M09 approvals (admin)" | Out-Null
    Invoke-Api -Method GET -Path "/api/v1/admin/audit-logs?page=1&pageSize=5" -Headers $h -Name "M16 audit logs"    | Out-Null
    Invoke-Api -Method GET -Path "/api/v1/admin/stores"                -Headers $h -Name "M03 stores"             | Out-Null

    Write-Host "`n-- RBAC negative --" -ForegroundColor Cyan
    Invoke-Api -Method GET -Path "/api/v1/admin/users" -Name "Admin endpoint WITHOUT token -> 401" -ExpectedStatus 401 | Out-Null
}
catch {
    Write-Host ""
    Write-Host "FATAL: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
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
