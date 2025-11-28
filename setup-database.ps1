# Quick Database Setup Script
# Run this in PowerShell from the solution root

Write-Host "?? Setting up LocalDB database..." -ForegroundColor Green

# Change to project directory
Set-Location "IT-Project2526"

Write-Host "`n?? Building project..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful!`n" -ForegroundColor Green
    
    Write-Host "??? Updating database..." -ForegroundColor Yellow
    
    # Try to update database
    $result = dotnet ef database update 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Database updated successfully!`n" -ForegroundColor Green
        Write-Host "?? You're ready to go! Run 'dotnet run' to start the app." -ForegroundColor Cyan
    } else {
        Write-Host "? Database update failed. Error:" -ForegroundColor Red
        Write-Host $result
        Write-Host "`n?? Try running in Visual Studio Package Manager Console:" -ForegroundColor Yellow
        Write-Host "   Update-Database" -ForegroundColor White
    }
} else {
    Write-Host "? Build failed. Please fix build errors first." -ForegroundColor Red
}

Set-Location ..
