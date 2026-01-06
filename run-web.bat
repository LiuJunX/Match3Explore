@echo off
set PORT=5015

echo Checking port %PORT%...

for /f "tokens=5" %%a in ('netstat -aon ^| findstr :%PORT%') do (
    echo Port %PORT% is in use by PID %%a. Killing it...
    taskkill /F /PID %%a >nul 2>&1
)

echo Starting Web Project with Hot Reload...
dotnet watch --project src/Match3.Web/Match3.Web/Match3.Web.csproj
