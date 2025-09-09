# Railway Deployment Script for Notetaker (PowerShell)
Write-Host "Starting Railway deployment for Notetaker..." -ForegroundColor Green

# Check if Railway CLI is installed
try {
    railway --version | Out-Null
    Write-Host "Railway CLI is ready" -ForegroundColor Green
} catch {
    Write-Host "Railway CLI not found. Please install it first:" -ForegroundColor Red
    Write-Host "   npm install -g @railway/cli" -ForegroundColor Yellow
    Write-Host "   or visit: https://docs.railway.app/develop/cli" -ForegroundColor Yellow
    exit 1
}

# Check if user is logged in
try {
    railway whoami | Out-Null
    Write-Host "Logged in to Railway" -ForegroundColor Green
} catch {
    Write-Host "Please log in to Railway first:" -ForegroundColor Yellow
    Write-Host "   railway login" -ForegroundColor Yellow
    exit 1
}

# Check if project is linked
if (Test-Path ".railway/project.json") {
    Write-Host "Project already linked" -ForegroundColor Green
} else {
    Write-Host "Please link to Railway project:" -ForegroundColor Yellow
    Write-Host "   railway link" -ForegroundColor Yellow
    Write-Host "   Then select your project or create a new one" -ForegroundColor Yellow
    exit 1
}

# Display environment variables needed
Write-Host "`nEnvironment Variables Required:" -ForegroundColor Cyan
Write-Host "   Set these in your Railway dashboard:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Database:" -ForegroundColor White
Write-Host "   - DATABASE_HOST" -ForegroundColor Gray
Write-Host "   - DATABASE_NAME" -ForegroundColor Gray
Write-Host "   - DATABASE_USER" -ForegroundColor Gray
Write-Host "   - DATABASE_PASSWORD" -ForegroundColor Gray
Write-Host "   - DATABASE_PORT" -ForegroundColor Gray
Write-Host ""
Write-Host "   JWT:" -ForegroundColor White
Write-Host "   - JWT_SIGNING_KEY" -ForegroundColor Gray
Write-Host ""
Write-Host "   Google OAuth:" -ForegroundColor White
Write-Host "   - GOOGLE_CLIENT_ID" -ForegroundColor Gray
Write-Host "   - GOOGLE_CLIENT_SECRET" -ForegroundColor Gray
Write-Host ""
Write-Host "   External APIs:" -ForegroundColor White
Write-Host "   - RECALL_AI_API_KEY" -ForegroundColor Gray
Write-Host "   - OPENAI_API_KEY" -ForegroundColor Gray
Write-Host "   - LINKEDIN_CLIENT_ID" -ForegroundColor Gray
Write-Host "   - LINKEDIN_CLIENT_SECRET" -ForegroundColor Gray
Write-Host "   - FACEBOOK_APP_ID" -ForegroundColor Gray
Write-Host "   - FACEBOOK_APP_SECRET" -ForegroundColor Gray
Write-Host ""

# Deploy the application
Write-Host "Deploying to Railway..." -ForegroundColor Green
railway up

Write-Host "`nDeployment initiated!" -ForegroundColor Green
Write-Host "Monitor your deployment at: https://railway.app" -ForegroundColor Cyan
Write-Host "Check logs with: railway logs" -ForegroundColor Cyan
Write-Host "Your app will be available at the URL shown in the Railway dashboard" -ForegroundColor Cyan