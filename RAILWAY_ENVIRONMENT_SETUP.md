# Railway Environment Variables Setup Guide

This guide will walk you through setting up all the required environment variables in your Railway dashboard.

## 🚀 Step-by-Step Instructions

### Step 1: Access Your Railway Project
1. Go to [railway.app](https://railway.app) and log in
2. Click on your Notetaker project
3. You should see your deployed service

### Step 2: Navigate to Environment Variables
1. Click on your **service** (the one running your app)
2. Click on the **"Variables"** tab at the top
3. You'll see a section called **"Environment Variables"**

### Step 3: Add Each Environment Variable

Click **"+ New Variable"** for each variable below:

#### Database Configuration
```
Variable Name: DATABASE_HOST
Value: [Your PostgreSQL host from Railway database]

Variable Name: DATABASE_NAME  
Value: notetaker

Variable Name: DATABASE_USER
Value: [Your PostgreSQL username from Railway database]

Variable Name: DATABASE_PASSWORD
Value: [Your PostgreSQL password from Railway database]

Variable Name: DATABASE_PORT
Value: 5432
```

#### JWT Configuration
```
Variable Name: JWT_SIGNING_KEY
Value: [Generate a secure 32+ character string]
Example: your-super-secret-jwt-key-at-least-32-characters-long-12345
```

#### Google OAuth Configuration
```
Variable Name: GOOGLE_CLIENT_ID
Value: [Your Google OAuth Client ID from Google Cloud Console]

Variable Name: GOOGLE_CLIENT_SECRET
Value: [Your Google OAuth Client Secret from Google Cloud Console]
```

#### External API Keys
```
Variable Name: RECALL_AI_API_KEY
Value: [Your Recall.ai API key]

Variable Name: OPENAI_API_KEY
Value: [Your OpenAI API key]

Variable Name: LINKEDIN_CLIENT_ID
Value: [Your LinkedIn App Client ID]

Variable Name: LINKEDIN_CLIENT_SECRET
Value: [Your LinkedIn App Client Secret]

Variable Name: FACEBOOK_APP_ID
Value: [Your Facebook App ID]

Variable Name: FACEBOOK_APP_SECRET
Value: [Your Facebook App Secret]
```

## 🔍 How to Get Database Connection Details

### If you haven't added PostgreSQL yet:
1. In your Railway project, click **"+ New"**
2. Select **"Database"** → **"PostgreSQL"**
3. Railway will create the database and show connection details

### To get existing database details:
1. Click on your **PostgreSQL service** in Railway
2. Go to the **"Variables"** tab
3. Copy these values:
   - `PGHOST` → Use as `DATABASE_HOST`
   - `PGDATABASE` → Use as `DATABASE_NAME` 
   - `PGUSER` → Use as `DATABASE_USER`
   - `PGPASSWORD` → Use as `DATABASE_PASSWORD`
   - `PGPORT` → Use as `DATABASE_PORT`

## 🔐 How to Get API Keys

### Google OAuth (Required)
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API and Google Calendar API
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client ID"
5. Set authorized redirect URI: `https://your-app-url.up.railway.app/api/auth/google/callback`

### Recall.ai API (Required)
1. Sign up at [recall.ai](https://recall.ai)
2. Go to your dashboard
3. Copy your API key

### OpenAI API (Required)
1. Sign up at [OpenAI](https://platform.openai.com)
2. Go to API Keys section
3. Create a new API key

### LinkedIn API (Optional)
1. Go to [LinkedIn Developer Portal](https://www.linkedin.com/developers/)
2. Create a new app
3. Get Client ID and Client Secret

### Facebook API (Optional)
1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create a new app
3. Get App ID and App Secret

## ✅ Verification

After adding all variables:
1. Click **"Deploy"** or your app will auto-redeploy
2. Check the **"Logs"** tab to ensure no environment variable errors
3. Visit your app URL to test functionality

## 🚨 Important Notes

- **Never commit API keys to Git** - only set them in Railway dashboard
- **Use strong, unique values** for JWT_SIGNING_KEY
- **Update redirect URIs** in OAuth providers to match your Railway domain
- **Test each integration** after setting up the variables

## 🔄 Updating Variables

To update any variable:
1. Go to the **"Variables"** tab
2. Click the **pencil icon** next to the variable
3. Update the value
4. Click **"Save"**
5. Your app will automatically redeploy

## 🆘 Troubleshooting

### Common Issues:
- **"Variable not found"** - Check spelling and case sensitivity
- **"Database connection failed"** - Verify database variables are correct
- **"OAuth redirect mismatch"** - Update redirect URIs in OAuth providers
- **"API key invalid"** - Verify API keys are correct and have proper permissions

### Check Logs:
1. Go to your service
2. Click **"Deployments"**
3. Click on the latest deployment
4. Check the **"Logs"** tab for error messages
