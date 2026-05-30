<#
.SYNOPSIS
  Xoá dữ liệu user test do smoke-day4.ps1 tạo ra.

.DESCRIPTION
  Hard-delete các user khớp pattern email test (mặc định 'smoke.%@example.com')
  cùng toàn bộ bản ghi phụ thuộc trong các bảng auth.

  KHÔNG đụng tới audit_log: theo CR-1 / ADR, audit log là append-only.
  Các dòng audit của user test sẽ được giữ lại (đúng thiết kế).

  Chạy qua `docker exec` vào container rmms-postgres, nên cần Docker đang chạy
  và service postgres đã up.

.EXAMPLE
  .\scripts\cleanup-smoke.ps1
  .\scripts\cleanup-smoke.ps1 -EmailPattern 'smoke.%@example.com' -DryRun
#>
[CmdletBinding()]
param(
    [string]$Container    = "rmms-postgres",
    [string]$DbUser       = "rmms",
    [string]$DbName       = "rmms",
    [string]$EmailPattern = "smoke.%@example.com",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "RMMS smoke-data cleanup" -ForegroundColor Cyan
Write-Host ("-" * 70)
Write-Host "Container    = $Container"
Write-Host "Database     = $DbName (user $DbUser)"
Write-Host "EmailPattern = $EmailPattern"
Write-Host "DryRun       = $DryRun"
Write-Host ("-" * 70)

# Thứ tự xoá tôn trọng FK: bảng con trước, users sau cùng.
# audit_log CỐ TÌNH bị loại trừ (append-only).
$childTables = @(
    "refresh_tokens",
    "login_history",
    "user_devices",
    "email_verification_tokens",
    "password_reset_tokens"
)

$sql = @"
\set ON_ERROR_STOP on
BEGIN;

-- Liệt kê user sắp xoá
SELECT id, email, role, status FROM users WHERE email LIKE '$EmailPattern' ORDER BY email;

"@

foreach ($t in $childTables) {
    $sql += "DELETE FROM $t WHERE user_id IN (SELECT id FROM users WHERE email LIKE '$EmailPattern');`n"
}
$sql += "DELETE FROM users WHERE email LIKE '$EmailPattern';`n"

if ($DryRun) {
    $sql += "ROLLBACK;`n"
    Write-Host "DryRun: sẽ ROLLBACK, không xoá thật." -ForegroundColor Yellow
} else {
    $sql += "COMMIT;`n"
}

# Đếm trước khi xoá
$countCmd = "SELECT count(*) FROM users WHERE email LIKE '$EmailPattern';"
$before = (docker exec -i $Container psql -U $DbUser -d $DbName -tAc $countCmd).Trim()
Write-Host "User test khớp pattern hiện có: $before" -ForegroundColor DarkGray

if ($before -eq "0") {
    Write-Host "Không có gì để xoá. Kết thúc." -ForegroundColor Green
    exit 0
}

# Thực thi
$sql | docker exec -i $Container psql -U $DbUser -d $DbName -v ON_ERROR_STOP=1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Cleanup THẤT BẠI (exit $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not $DryRun) {
    $after = (docker exec -i $Container psql -U $DbUser -d $DbName -tAc $countCmd).Trim()
    Write-Host ("-" * 70)
    Write-Host "Đã xoá. User test còn lại: $after" -ForegroundColor Green
    Write-Host "(audit_log được giữ nguyên theo thiết kế append-only.)" -ForegroundColor DarkGray
}
