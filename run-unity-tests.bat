@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  Match3 Unity Editor Tests Runner
echo ============================================
echo.

set PROJECT_PATH=%~dp0unity
set RESULTS_FILE=%~dp0unity-test-results.xml

:: Find Unity Editor
set UNITY_EXE=
set UNITY_HUB_PATH=C:\Program Files\Unity\Hub\Editor

:: Check for Unity installation
if exist "%UNITY_HUB_PATH%" (
    for /d %%d in ("%UNITY_HUB_PATH%\*") do (
        if exist "%%d\Editor\Unity.exe" (
            set UNITY_EXE=%%d\Editor\Unity.exe
        )
    )
)

:: Fallback: check environment variable
if "%UNITY_EXE%"=="" (
    if not "%UNITY_EDITOR_PATH%"=="" (
        set UNITY_EXE=%UNITY_EDITOR_PATH%
    )
)

if "%UNITY_EXE%"=="" (
    echo [ERROR] Unity Editor not found.
    echo.
    echo Please either:
    echo   1. Install Unity via Unity Hub to default location
    echo   2. Set UNITY_EDITOR_PATH environment variable
    echo.
    echo Example: set UNITY_EDITOR_PATH=C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe
    exit /b 1
)

echo Found Unity: %UNITY_EXE%
echo Project: %PROJECT_PATH%
echo.

echo [1/2] Running Editor Tests...
echo.

"%UNITY_EXE%" -runTests -batchmode -projectPath "%PROJECT_PATH%" -testResults "%RESULTS_FILE%" -testPlatform EditMode

set TEST_EXIT_CODE=%ERRORLEVEL%

echo.
echo [2/2] Parsing Results...
echo.

if exist "%RESULTS_FILE%" (
    :: Extract test counts from XML
    for /f "tokens=2 delims==" %%a in ('findstr /i "total=" "%RESULTS_FILE%" 2^>nul') do (
        set TOTAL=%%a
        set TOTAL=!TOTAL:"=!
        set TOTAL=!TOTAL: =!
        goto :found_total
    )
    :found_total

    for /f "tokens=2 delims==" %%a in ('findstr /i "passed=" "%RESULTS_FILE%" 2^>nul') do (
        set PASSED=%%a
        set PASSED=!PASSED:"=!
        set PASSED=!PASSED: =!
        goto :found_passed
    )
    :found_passed

    for /f "tokens=2 delims==" %%a in ('findstr /i "failed=" "%RESULTS_FILE%" 2^>nul') do (
        set FAILED=%%a
        set FAILED=!FAILED:"=!
        set FAILED=!FAILED: =!
        goto :found_failed
    )
    :found_failed

    echo ============================================
    if %TEST_EXIT_CODE% equ 0 (
        echo  [PASS] All tests passed!
    ) else (
        echo  [FAIL] Some tests failed!
    )
    echo ============================================
    echo.
    echo Results: %RESULTS_FILE%
    echo.

    :: Show failed tests if any
    if %TEST_EXIT_CODE% neq 0 (
        echo Failed Tests:
        findstr /i "<test-case.*result=\"Failed\"" "%RESULTS_FILE%"
    )
) else (
    echo [ERROR] Test results file not generated.
    echo Unity may have failed to start or project has errors.
)

exit /b %TEST_EXIT_CODE%
