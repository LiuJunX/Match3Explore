# Define the port to check
$port = 5015

# Get the process ID using the port
$process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique

if ($process) {
    Write-Host "Port $port is in use by process ID $process. Killing it..." -ForegroundColor Yellow
    Stop-Process -Id $process -Force -ErrorAction SilentlyContinue
    Write-Host "Process killed." -ForegroundColor Green
} else {
    Write-Host "Port $port is free." -ForegroundColor Green
}

# Start dotnet watch
Write-Host "Starting Web Project with Hot Reload..." -ForegroundColor Cyan
dotnet watch --project src/Match3.Web/Match3.Web/Match3.Web.csproj
