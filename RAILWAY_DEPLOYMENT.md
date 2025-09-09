# Railway Deployment Guide for Notetaker

This guide will help you deploy the Notetaker application to Railway with both the .NET API backend and Angular frontend.

## 🚀 Prerequisites

1. **Railway Account**: Sign up at [railway.app](https://railway.app)
2. **GitHub Repository**: Push your code to GitHub
3. **API Keys**: Gather all required API keys (see Environment Variables section)

## 📋 Environment Variables

Set these environment variables in your Railway project:

### Database Configuration
```
DATABASE_HOST=your-postgres-host
DATABASE_NAME=notetaker
DATABASE_USER=postgres
DATABASE_PASSWORD=your-postgres-password
DATABASE_PORT=5432
```

### JWT Configuration
```
JWT_SIGNING_KEY=your-super-secret-jwt-key-at-least-32-characters-long
```

### Google OAuth
```
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
```

### External APIs
```
RECALL_AI_API_KEY=your-recall-ai-api-key
OPENAI_API_KEY=your-openai-api-key
LINKEDIN_CLIENT_ID=your-linkedin-client-id
LINKEDIN_CLIENT_SECRET=your-linkedin-client-secret
FACEBOOK_APP_ID=your-facebook-app-id
FACEBOOK_APP_SECRET=your-facebook-app-secret
```

## 🏗️ Deployment Steps

### Step 1: Create Railway Project

1. Go to [railway.app](https://railway.app) and create a new project
2. Connect your GitHub repository
3. Select "Deploy from GitHub repo"

### Step 2: Add PostgreSQL Database

1. In your Railway project, click "New"
2. Select "Database" → "PostgreSQL"
3. Railway will automatically create the database and provide connection details
4. Copy the connection details to set up your environment variables

### Step 3: Configure Environment Variables

1. Go to your service settings
2. Navigate to "Variables" tab
3. Add all the environment variables listed above
4. Make sure to update the URLs to match your Railway domain

### Step 4: Deploy the Application

1. Railway will automatically detect the Dockerfile and start building
2. The build process will:
   - Build the Angular frontend with Railway configuration
   - Build the .NET API
   - Create a production-ready container

### Step 5: Configure Custom Domain (Optional)

1. Go to your service settings
2. Navigate to "Settings" → "Domains"
3. Add your custom domain
4. Update CORS settings in `Program.cs` if needed

## 🔧 Configuration Files

### Railway Configuration (`railway.json`)
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "cd notetaker/Notetaker.Api && dotnet run --urls=http://0.0.0.0:$PORT",
    "healthcheckPath": "/api/health",
    "healthcheckTimeout": 100,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

### Dockerfile
The Dockerfile uses a multi-stage build:
1. **Frontend Stage**: Builds Angular app with Railway configuration
2. **API Stage**: Builds .NET API
3. **Runtime Stage**: Combines both and serves static files

### Environment Configuration
- `environment.railway.ts`: Frontend configuration for Railway
- `appsettings.Production.json`: Backend configuration with environment variables

## 🚨 Troubleshooting

### Common Issues

1. **Build Failures**
   - Check that all environment variables are set
   - Verify Node.js and .NET versions in Dockerfile
   - Check build logs in Railway dashboard

2. **Database Connection Issues**
   - Verify database environment variables
   - Check that PostgreSQL service is running
   - Ensure database exists and is accessible

3. **CORS Issues**
   - Update CORS origins in `Program.cs`
   - Verify frontend URL matches your Railway domain

4. **API Key Issues**
   - Double-check all API keys are correctly set
   - Verify external service URLs are accessible
   - Check API key permissions and scopes

### Health Check

The application includes a health check endpoint at `/api/health` that Railway uses to monitor the service.

### Logs

View logs in the Railway dashboard:
1. Go to your service
2. Click on "Deployments"
3. Select the latest deployment
4. View logs in the "Logs" tab

## 🔄 Updates and Redeployment

1. Push changes to your GitHub repository
2. Railway will automatically detect changes and redeploy
3. Monitor the deployment in the Railway dashboard
4. Check logs if deployment fails

## 📊 Monitoring

Railway provides built-in monitoring:
- **Metrics**: CPU, memory, and network usage
- **Logs**: Real-time application logs
- **Health Checks**: Automatic health monitoring
- **Alerts**: Configure alerts for failures

## 🔒 Security Considerations

1. **Environment Variables**: Never commit sensitive data to Git
2. **HTTPS**: Railway provides automatic HTTPS
3. **CORS**: Configure CORS properly for production
4. **API Keys**: Rotate API keys regularly
5. **Database**: Use strong passwords and restrict access

## 📈 Scaling

Railway supports automatic scaling:
1. Go to service settings
2. Navigate to "Scaling"
3. Configure auto-scaling rules
4. Set resource limits

## 🆘 Support

- **Railway Documentation**: [docs.railway.app](https://docs.railway.app)
- **Railway Discord**: [discord.gg/railway](https://discord.gg/railway)
- **GitHub Issues**: Create issues in your repository

## ✅ Post-Deployment Checklist

- [ ] Application is accessible via Railway URL
- [ ] Health check endpoint responds correctly
- [ ] Database connection is working
- [ ] All API endpoints are functional
- [ ] Frontend loads and displays correctly
- [ ] Authentication flow works
- [ ] External API integrations are working
- [ ] Logs are being generated correctly
- [ ] Monitoring is set up
- [ ] Custom domain is configured (if applicable)

---

**Note**: Remember to update the CORS origins in `Program.cs` with your actual Railway domain after deployment.
