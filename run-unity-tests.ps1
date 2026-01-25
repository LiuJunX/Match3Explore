<#
.SYNOPSIS
    Runs Unity Editor tests for Match3 project.

.DESCRIPTION
    Executes NUnit tests in Unity Editor's EditMode test runner.
    Outputs results in a format suitable for CI/CD pipelines.

.EXAMPLE
    .\run-unity-tests.ps1

.EXAMPLE
    .\run-unity-tests.ps1 -Verbose
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Match3 Unity Editor Tests Runner" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = Join-Path $PSScriptRoot "unity"
$resultsFile = Join-Path $PSScriptRoot "unity-test-results.xml"

# Find Unity Editor
$unityExe = $null
$unityHubPath = "C:\Program Files\Unity\Hub\Editor"

if (Test-Path $unityHubPath) {
    $versions = Get-ChildItem $unityHubPath -Directory | Sort-Object Name -Descending
    foreach ($version in $versions) {
        $exePath = Join-Path $version.FullName "Editor\Unity.exe"
        if (Test-Path $exePath) {
            $unityExe = $exePath
            break
        }
    }
}

# Fallback to environment variable
if (-not $unityExe -and $env:UNITY_EDITOR_PATH) {
    if (Test-Path $env:UNITY_EDITOR_PATH) {
        $unityExe = $env:UNITY_EDITOR_PATH
    }
}

if (-not $unityExe) {
    Write-Host "[ERROR] Unity Editor not found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please either:"
    Write-Host "  1. Install Unity via Unity Hub to default location"
    Write-Host "  2. Set UNITY_EDITOR_PATH environment variable"
    Write-Host ""
    exit 1
}

Write-Host "Unity: $unityExe" -ForegroundColor Gray
Write-Host "Project: $projectPath" -ForegroundColor Gray
Write-Host ""

# Remove old results
if (Test-Path $resultsFile) {
    Remove-Item $resultsFile -Force
}

Write-Host "[1/2] Running Editor Tests..." -ForegroundColor Yellow
Write-Host ""

$process = Start-Process -FilePath $unityExe -ArgumentList @(
    "-runTests",
    "-batchmode",
    "-projectPath", "`"$projectPath`"",
    "-testResults", "`"$resultsFile`"",
    "-testPlatform", "EditMode"
) -Wait -PassThru -NoNewWindow

$exitCode = $process.ExitCode

Write-Host ""
Write-Host "[2/2] Parsing Results..." -ForegroundColor Yellow
Write-Host ""

if (Test-Path $resultsFile) {
    [xml]$results = Get-Content $resultsFile

    $testRun = $results.'test-run'
    $total = [int]$testRun.total
    $passed = [int]$testRun.passed
    $failed = [int]$testRun.failed
    $skipped = [int]$testRun.skipped
    $duration = [math]::Round([double]$testRun.duration, 2)

    Write-Host "============================================" -ForegroundColor Cyan
    if ($failed -eq 0) {
        Write-Host " [PASS] All $total tests passed! ($duration s)" -ForegroundColor Green
    } else {
        Write-Host " [FAIL] $failed of $total tests failed ($duration s)" -ForegroundColor Red
    }
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Passed:  $passed" -ForegroundColor Green
    Write-Host "  Failed:  $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Gray" })
    Write-Host "  Skipped: $skipped" -ForegroundColor $(if ($skipped -gt 0) { "Yellow" } else { "Gray" })
    Write-Host ""

    # Show failed tests
    if ($failed -gt 0) {
        Write-Host "Failed Tests:" -ForegroundColor Red
        Write-Host ""

        $failedTests = $results.SelectNodes("//test-case[@result='Failed']")
        foreach ($test in $failedTests) {
            Write-Host "  - $($test.fullname)" -ForegroundColor Red
            $message = $test.failure.message
            if ($message) {
                Write-Host "    $message" -ForegroundColor DarkRed
            }
        }
        Write-Host ""
    }

    # List all tests
    Write-Verbose "All Tests:"
    $allTests = $results.SelectNodes("//test-case")
    foreach ($test in $allTests) {
        $status = switch ($test.result) {
            "Passed" { "[PASS]" }
            "Failed" { "[FAIL]" }
            "Skipped" { "[SKIP]" }
            default { "[????]" }
        }
        Write-Verbose "  $status $($test.name)"
    }

    Write-Host "Results: $resultsFile" -ForegroundColor Gray
} else {
    Write-Host "[ERROR] Test results file not generated." -ForegroundColor Red
    Write-Host "Unity may have failed to start or project has compilation errors." -ForegroundColor Red
    exit 1
}

exit $exitCode
