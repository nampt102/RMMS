# RMMS - Fix CI failures on chore/ci-bootstrap PR
# ----------------------------------------------------------------------------
# Diagnoses fixed in this commit:
#
#   1) backend / Restore -> MSB1008: Only one project can be specified
#      Root cause: `dotnet restore Rmms.sln --locked-mode=false`
#      MSBuild parses `false` as a 2nd project argument.
#      Fix: drop the `--locked-mode=false` flag (default behavior is fine).
#      File: .github/workflows/backend.yml
#
#   2) web / Run tests (vitest) -> "No test files found, exiting with code 1"
#      Vitest exits non-zero when there are zero test files. The scaffold has
#      none yet (real tests arrive with M01+).
#      Fix: add `--passWithNoTests` to the `test` script.
#      File: web/package.json
#
#   3) mobile / Get dependencies -> "retrofit_generator 9.7.0 requires SDK
#      version >=3.6.0 <4.0.0, version solving failed"
#      Flutter 3.22 (per tech stack) ships Dart 3.4.4. retrofit_generator
#      9.7.0 raised its SDK floor to >=3.6.0.
#      Fix: relax constraint to ^9.1.0 so pub picks the latest 9.x compatible
#      with Dart 3.4.4. Also remove pubspec.lock so flutter pub get can re-
#      solve cleanly on the next install.
#      Files: mobile/pubspec.yaml, mobile/pubspec.lock (deleted)
#
# How to run:
#   cd D:\WORKING\SOURCECODE\RMMS
#   powershell -ExecutionPolicy Bypass -File .\scripts\fix-ci-failures.ps1
# ----------------------------------------------------------------------------

$ErrorActionPreference = "Stop"

function Step($n, $msg) {
    Write-Host ""
    Write-Host "==> [$n] $msg" -ForegroundColor Cyan
}

Set-Location -Path $PSScriptRoot\..

Step 1 "Sanity check - must be on chore/ci-bootstrap"
$branch = (git branch --show-current).Trim()
if ($branch -ne "chore/ci-bootstrap") {
    Write-Error "Currently on '$branch'. Please: git checkout chore/ci-bootstrap"
}
Write-Host "OK - on chore/ci-bootstrap"

Step 2 "Pull latest (in case the branch has new commits on origin)"
git pull --ff-only origin chore/ci-bootstrap

Step 3 "Verify the 3 edits are present in the working tree"
$backendOk = (Select-String -Path ".github\workflows\backend.yml" -Pattern "dotnet restore Rmms\.sln$" -Quiet)
$webOk     = (Select-String -Path "web\package.json"              -Pattern "vitest run --passWithNoTests" -Quiet)
$mobileOk  = (Select-String -Path "mobile\pubspec.yaml"           -Pattern "retrofit_generator: \^9\.1\.0" -Quiet)

if (-not $backendOk) { Write-Error "backend.yml edit missing - was the file synced from the AI session?" }
if (-not $webOk)     { Write-Error "web/package.json edit missing - was the file synced from the AI session?" }
if (-not $mobileOk)  { Write-Error "mobile/pubspec.yaml edit missing - was the file synced from the AI session?" }
Write-Host "All 3 edits found"

Step 4 "Remove mobile/pubspec.lock (force re-resolve with new constraint)"
if (Test-Path "mobile\pubspec.lock") {
    git rm -f mobile/pubspec.lock
    Write-Host "mobile/pubspec.lock removed from index + working tree"
} else {
    Write-Host "mobile/pubspec.lock already absent - skip"
}

Step 5 "Stage the 3 fixed files"
git add .github/workflows/backend.yml web/package.json mobile/pubspec.yaml

Write-Host ""
Write-Host "Staged files:"
git diff --cached --name-status

Step 6 "Commit"
$msg = @"
fix(ci): unblock 3 failing jobs on the chore/ci-bootstrap PR

backend.yml:
  Drop `--locked-mode=false` from `dotnet restore`. MSBuild was parsing the
  value `false` as a second project arg, triggering MSB1008. We don't have
  packages.lock.json files yet, so locked mode wasn't doing anything useful.

web/package.json:
  Add `--passWithNoTests` to the `test` script. Vitest exits with code 1
  when the scaffold has zero test files; real tests land with M01 onward.

mobile/pubspec.yaml + pubspec.lock:
  Relax `retrofit_generator: 9.7.0` to `^9.1.0`. 9.7.0 requires Dart
  >=3.6.0, but Flutter 3.22 (per tech stack) ships Dart 3.4.4. Lock file
  removed so `flutter pub get` resolves the highest compatible 9.x.

After this commit, all three workflows should reach green on the
chore/ci-bootstrap PR, closing out Sprint 00.
"@

git commit -m $msg

Step 7 "Push"
git push origin chore/ci-bootstrap

Step 8 "DONE"
$repoUrl = (git config --get remote.origin.url) -replace '\.git$',''
$prUrl   = "$repoUrl/pull/1"  # PR number unknown from CLI; user can refresh existing tab
Write-Host ""
Write-Host "Refresh the PR in your browser. The 3 workflows should re-run automatically." -ForegroundColor Green
Write-Host "Expected: all 3 jobs green within ~3-7 minutes."
Write-Host ""
Write-Host "Repo URL: $repoUrl"
