# RMMS — Sprint 00 → Sprint 01 CI bootstrap
# ----------------------------------------------------------------------------
# Mục đích:
#   1) Commit & push lên main:
#        - .github/workflows/{backend,web,mobile}.yml  (CI/CD mới)
#        - knowledge-base/decisions/ADR-001..008.md   (8 ADRs Accepted)
#        - knowledge-base/decisions/README.md         (ADR index updated)
#        - knowledge-base/PROJECT-STATE.md            (~85% progress)
#        - knowledge-base/CHANGELOG.md                (entry mới)
#        - knowledge-base/prompts/system-prompt.md    (sync với .cursor rule)
#        - .cursor/rules/rmms.mdc                     (sync với system prompt)
#   2) Tạo nhánh chore/ci-bootstrap với 3 file ".ci-trigger" nhỏ để
#      kích hoạt cả 3 workflows trên PR đầu tiên.
#   3) Push nhánh và in URL mở PR.
#
# KHÔNG đụng vào:
#   - mobile/** (87 file đang "modified" chỉ là noise CRLF/LF, sẽ xử lý sau
#     bằng .gitattributes ở một PR riêng)
#   - ~$RMMS-Phase1-Plan.xlsx (lock file của Excel)
#
# Cách chạy:
#   cd D:\WORKING\SOURCECODE\RMMS
#   powershell -ExecutionPolicy Bypass -File .\scripts\ci-bootstrap.ps1
# ----------------------------------------------------------------------------

$ErrorActionPreference = "Stop"

function Step($n, $msg) {
    Write-Host ""
    Write-Host "==> [$n] $msg" -ForegroundColor Cyan
}

# Đảm bảo ta đang ở repo root
Set-Location -Path $PSScriptRoot\..

Step 1 "Sanity check"
git rev-parse --is-inside-work-tree | Out-Null
$branch = (git branch --show-current).Trim()
if ($branch -ne "main") {
    Write-Error "Đang ở nhánh '$branch'. Hãy checkout main trước khi chạy script này."
}
Write-Host "OK — repo + branch main"

Step 2 "Set git identity (nếu chưa có)"
$cfgName  = (git config user.name) 2>$null
$cfgEmail = (git config user.email) 2>$null
if ([string]::IsNullOrWhiteSpace($cfgName))  { git config user.name  "James" }
if ([string]::IsNullOrWhiteSpace($cfgEmail)) { git config user.email "jamesphan@motivesvn.com" }
Write-Host ("user.name  = " + (git config user.name))
Write-Host ("user.email = " + (git config user.email))

Step 3 "Stage workflows + ADRs + KB updates (KHÔNG stage mobile/* CRLF noise)"
git add `
    .github/workflows/backend.yml `
    .github/workflows/web.yml `
    .github/workflows/mobile.yml

git add `
    knowledge-base/decisions/ADR-001-modular-monolith.md `
    knowledge-base/decisions/ADR-002-mediator-martin-othmar.md `
    knowledge-base/decisions/ADR-003-uuid-v7-app-generated.md `
    knowledge-base/decisions/ADR-004-soft-delete-interceptor.md `
    knowledge-base/decisions/ADR-005-snake-case-postgres.md `
    knowledge-base/decisions/ADR-006-postgis-deferred.md `
    knowledge-base/decisions/ADR-007-caddy-reverse-proxy.md `
    knowledge-base/decisions/ADR-008-tailwind-preflight-disabled.md `
    knowledge-base/decisions/README.md

git add `
    knowledge-base/PROJECT-STATE.md `
    knowledge-base/CHANGELOG.md `
    knowledge-base/prompts/system-prompt.md `
    .cursor/rules/rmms.mdc

Write-Host ""
Write-Host "Files đã staged:"
git diff --cached --name-status

Step 4 "Commit lên main"
$msg1 = @"
chore(ci+adr): bootstrap GitHub Actions workflows and finalize ADR-001..008

- Add CI workflows for backend (.NET 10), web (Next.js 14), mobile (Flutter 3.22).
  All 3 use path filters + concurrency control + dependency caching.
  Backend workflow includes Postgres 16 + Redis 7 service containers for
  integration tests; format check via dotnet format --verify-no-changes.

- Promote 8 previously-informal architecture decisions to Accepted ADRs:
  ADR-001 Modular Monolith, ADR-002 Mediator (Martin Othmar fork),
  ADR-003 UUID v7 app-generated, ADR-004 Soft delete interceptor,
  ADR-005 snake_case Postgres, ADR-006 PostGIS deferred (NTS Haversine),
  ADR-007 Caddy reverse proxy, ADR-008 Tailwind preflight disabled.

- Sync .cursor/rules/rmms.mdc with knowledge-base/prompts/system-prompt.md
  (both reference PROJECT-STATE.md + ADR-001..009).

- PROJECT-STATE.md: Sprint 00 progress 70% -> 85%; only outstanding piece
  before Sprint 01/M01 is the first EF Core migration.

Refs: ADR-001..008, ADR-009.
"@

git commit -m $msg1

Step 5 "Push lên origin/main"
git push origin main

Step 6 "Tạo nhánh chore/ci-bootstrap"
git checkout -b chore/ci-bootstrap

Step 7 "Tạo 3 file .ci-trigger để kích hoạt cả 3 workflows"
# Mỗi file nằm trong path filter của workflow tương ứng.
$timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssK")

@"
# CI trigger marker

This file exists to trigger the backend GitHub Actions workflow on the
``chore/ci-bootstrap`` PR. It can be safely deleted in a follow-up PR
once CI is confirmed green.

Generated at: $timestamp
"@ | Set-Content -Path "backend\.ci-trigger.md" -Encoding UTF8

@"
# CI trigger marker

This file exists to trigger the web GitHub Actions workflow on the
``chore/ci-bootstrap`` PR. It can be safely deleted in a follow-up PR
once CI is confirmed green.

Generated at: $timestamp
"@ | Set-Content -Path "web\.ci-trigger.md" -Encoding UTF8

@"
# CI trigger marker

This file exists to trigger the mobile GitHub Actions workflow on the
``chore/ci-bootstrap`` PR. It can be safely deleted in a follow-up PR
once CI is confirmed green.

Generated at: $timestamp
"@ | Set-Content -Path "mobile\.ci-trigger.md" -Encoding UTF8

git add backend/.ci-trigger.md web/.ci-trigger.md mobile/.ci-trigger.md

Step 8 "Commit + push nhánh chore/ci-bootstrap"
$msg2 = @"
chore(ci): noop trigger files for first CI run verification

Adds .ci-trigger.md under backend/, web/, mobile/ so the three workflows
(backend.yml, web.yml, mobile.yml) all fire on this PR — required as the
first end-to-end check that:
  - actions/setup-dotnet@v4 resolves .NET 10
  - pnpm 9.15 + Node 20 cache works
  - subosito/flutter-action@v2 resolves Flutter 3.22.x
  - Postgres 16 + Redis 7 service containers come up healthy
  - format / lint / build / test all pass on a clean checkout

These files will be removed in a follow-up PR after CI is confirmed green.
"@

git commit -m $msg2
git push -u origin chore/ci-bootstrap

Step 9 "DONE — mở PR theo link sau"
$repoUrl = (git config --get remote.origin.url) -replace '\.git$',''
$prUrl   = "$repoUrl/compare/main...chore/ci-bootstrap?expand=1"
Write-Host ""
Write-Host "PR URL:"  -ForegroundColor Green
Write-Host $prUrl     -ForegroundColor Green
Write-Host ""
Write-Host "Sau khi mở PR, kiểm tra tab Checks — phải thấy 3 jobs:"
Write-Host "  - backend / Build + test (.NET 10)"
Write-Host "  - web / Build + test (Next.js 14)"
Write-Host "  - mobile / Analyze + test (Flutter 3.22)"
Write-Host ""
Write-Host "Khi cả 3 đều xanh -> báo lại cho AI session để cập nhật PROJECT-STATE.md và đóng Sprint 00."
