# PowerShell script to commit and deploy changes
# Usage: .\deploy-changes.ps1

try {
    Write-Host "Adding modified files to git..." -ForegroundColor Cyan
    git add src/ASPNETCRUD.API/Middleware/ExceptionHandlerMiddleware.cs
    git add src/ASPNETCRUD.API/Controllers/AuthController.cs
    git add src/ASPNETCRUD.Infrastructure/Services/AuthService.cs
    git add src/ASPNETCRUD.API/Program.cs
    git add src/ASPNETCRUD.Application/Interfaces/IAuthService.cs

    Write-Host "Committing changes..." -ForegroundColor Cyan
    git commit -m "feat: Add diagnostic logging and debugging tools"

    Write-Host "Changes committed successfully!" -ForegroundColor Green
    
    Write-Host "`nTo test the API, run:" -ForegroundColor Yellow
    Write-Host "  1. Deploy the changes to Railway" -ForegroundColor Yellow
    Write-Host "  2. Visit https://dotnetcrud-production.up.railway.app/api/Auth/diagnostic" -ForegroundColor Yellow
    Write-Host "  3. Check the Railway logs for detailed diagnostic information" -ForegroundColor Yellow
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
} 