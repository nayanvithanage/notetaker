# Railway Database & Environment Setup Guide

## 🗄️ Step 1: Add PostgreSQL Database

### In Railway Dashboard:
1. **Go to your Railway project**
2. **Click "+ New"** button
3. **Select "Database"** → **"PostgreSQL"**
4. **Wait for database to be created** (takes 1-2 minutes)

### Get Database Connection Details:
1. **Click on your PostgreSQL service**
2. **Go to "Variables" tab**
3. **Copy these values:**
   - `PGHOST` → Use as `DATABASE_HOST`
   - `PGDATABASE` → Use as `DATABASE_NAME`
   - `PGUSER` → Use as `DATABASE_USER`
   - `PGPASSWORD` → Use as `DATABASE_PASSWORD`
   - `PGPORT` → Use as `DATABASE_PORT`

## 🔧 Step 2: Set Environment Variables

### Go to Your App Service:
1. **Click on your app service** (not the database)
2. **Go to "Variables" tab**
3. **Add these variables one by one:**

#### Database Variables (Required):
```
DATABASE_HOST = [from PostgreSQL service PGHOST]
DATABASE_NAME = [from PostgreSQL service PGDATABASE]
DATABASE_USER = [from PostgreSQL service PGUSER]
DATABASE_PASSWORD = [from PostgreSQL service PGPASSWORD]
DATABASE_PORT = [from PostgreSQL service PGPORT]
```

#### JWT Configuration (Required):
```
JWT_SIGNING_KEY = your-super-secret-jwt-key-at-least-32-characters-long-12345
```

#### Google OAuth (Required):
```
GOOGLE_CLIENT_ID = [your-google-client-id]
GOOGLE_CLIENT_SECRET = [your-google-client-secret]
```

#### External APIs (Required):
```
RECALL_AI_API_KEY = [your-recall-ai-api-key]
OPENAI_API_KEY = [your-openai-api-key]
```

#### Social Media APIs (Optional):
```
LINKEDIN_CLIENT_ID = [your-linkedin-client-id]
LINKEDIN_CLIENT_SECRET = [your-linkedin-client-secret]
FACEBOOK_APP_ID = [your-facebook-app-id]
FACEBOOK_APP_SECRET = [your-facebook-app-secret]
```

## 🔑 Step 3: Get API Keys

### Google OAuth Setup:
1. **Go to [Google Cloud Console](https://console.cloud.google.com/)**
2. **Create a new project** or select existing
3. **Enable APIs:**
   - Google+ API
   - Google Calendar API
4. **Create OAuth 2.0 credentials:**
   - Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client ID"
   - Application type: "Web application"
   - Authorized redirect URIs: `https://your-app-url.up.railway.app/api/auth/google/callback`
5. **Copy Client ID and Client Secret**

### Recall.ai API:
1. **Sign up at [recall.ai](https://recall.ai)**
2. **Go to your dashboard**
3. **Copy your API key**

### OpenAI API:
1. **Sign up at [OpenAI](https://platform.openai.com)**
2. **Go to API Keys section**
3. **Create a new API key**

## ✅ Step 4: Verify Setup

### Check Database Connection:
1. **Go to your app service**
2. **Click "Deployments" tab**
3. **Click on the latest deployment**
4. **Check "Logs" tab**
5. **Look for:**
   - ✅ "Database connection successful"
   - ✅ "Application starting up"
   - ❌ Any database connection errors

### Test Health Check:
1. **Get your app URL** from Railway dashboard
2. **Visit:** `https://your-app-url.up.railway.app/api/health`
3. **Should return:**
   ```json
   {
     "status": "healthy",
     "timestamp": "2025-01-09T...",
     "version": "1.0.0",
     "database": "connected"
   }
   ```

## 🚨 Troubleshooting

### Database Connection Issues:
- **Check environment variables** are set correctly
- **Verify PostgreSQL service** is running
- **Check variable names** are exactly as shown (case-sensitive)

### App Won't Start:
- **Check logs** for specific error messages
- **Verify all required environment variables** are set
- **Check JWT_SIGNING_KEY** is at least 32 characters

### Health Check Fails:
- **Visit the health check URL** directly
- **Check the response** for error details
- **Verify database connection** is working

## 📋 Quick Checklist

- [ ] PostgreSQL database added to Railway
- [ ] Database environment variables set
- [ ] JWT_SIGNING_KEY set (32+ characters)
- [ ] Google OAuth credentials set
- [ ] Recall.ai API key set
- [ ] OpenAI API key set
- [ ] App redeployed successfully
- [ ] Health check returns "healthy"
- [ ] Database shows "connected" in health check

## 🆘 Need Help?

If you're still having issues:
1. **Check Railway logs** for specific error messages
2. **Verify all environment variables** are set correctly
3. **Test database connection** using the health check endpoint
4. **Check that all API keys** are valid and have proper permissions
