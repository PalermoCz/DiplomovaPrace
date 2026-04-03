$proc = Start-Process dotnet -ArgumentList "run", "--no-launch-profile" -PassThru -NoNewWindow
Start-Sleep -Seconds 15
try {
    Write-Host "Invoking API..."
    Invoke-RestMethod -Uri "http://localhost:5000/api/import-bdg2" -Method Post | Write-Host
} catch {
    Write-Host "Error: $_"
}
Write-Host "Stopping app..."
Stop-Process -Id $proc.Id -Force
