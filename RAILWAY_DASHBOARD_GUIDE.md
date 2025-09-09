# Railway Dashboard - Visual Guide

## 📱 Railway Dashboard Navigation

### 1. Accessing Your Project
```
Railway Dashboard → Your Project → Your Service → Variables Tab
```

### 2. Finding the Variables Section
Once you're in your service:
- Look for tabs at the top: **"Deployments"**, **"Variables"**, **"Settings"**
- Click on **"Variables"** tab
- You'll see two sections:
  - **"Environment Variables"** (for your app)
  - **"Build Variables"** (for build process)

### 3. Adding Environment Variables

#### Visual Layout:
```
┌─────────────────────────────────────────────────────────┐
│ Variables Tab                                           │
├─────────────────────────────────────────────────────────┤
│ Environment Variables                                   │
│ ┌─────────────────┬─────────────────────────────────┐   │
│ │ Variable Name   │ Value                           │   │
│ ├─────────────────┼─────────────────────────────────┤   │
│ │ DATABASE_HOST   │ your-postgres-host              │   │
│ │ DATABASE_NAME   │ notetaker                       │   │
│ │ JWT_SIGNING_KEY │ your-secret-key                 │   │
│ └─────────────────┴─────────────────────────────────┘   │
│ [+ New Variable]                                        │
└─────────────────────────────────────────────────────────┘
```

#### Step-by-Step Process:
1. **Click "+ New Variable"**
2. **Enter Variable Name** (e.g., `DATABASE_HOST`)
3. **Enter Variable Value** (e.g., your database host)
4. **Click "Add"**
5. **Repeat for each variable**

## 🔧 Complete Variable List

### Required Variables (Must Have):
```
DATABASE_HOST=your-postgres-host
DATABASE_NAME=notetaker
DATABASE_USER=postgres
DATABASE_PASSWORD=your-postgres-password
DATABASE_PORT=5432
JWT_SIGNING_KEY=your-super-secret-jwt-key-at-least-32-characters-long
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
RECALL_AI_API_KEY=your-recall-ai-api-key
OPENAI_API_KEY=your-openai-api-key
```

### Optional Variables (For Full Functionality):
```
LINKEDIN_CLIENT_ID=your-linkedin-client-id
LINKEDIN_CLIENT_SECRET=your-linkedin-client-secret
FACEBOOK_APP_ID=your-facebook-app-id
FACEBOOK_APP_SECRET=your-facebook-app-secret
```

## 🎯 Quick Setup Checklist

### Phase 1: Basic Setup (Required)
- [ ] Add PostgreSQL database service
- [ ] Set database connection variables
- [ ] Set JWT signing key
- [ ] Set Google OAuth credentials
- [ ] Set Recall.ai API key
- [ ] Set OpenAI API key

### Phase 2: Full Features (Optional)
- [ ] Set LinkedIn API credentials
- [ ] Set Facebook API credentials

## 🚀 After Setting Variables

1. **Save Changes** - Variables are saved automatically
2. **Redeploy** - Your app will automatically redeploy
3. **Check Logs** - Monitor deployment in "Deployments" tab
4. **Test App** - Visit your app URL to verify functionality

## 🔍 Troubleshooting Dashboard Issues

### Can't Find Variables Tab?
- Make sure you're in your **service**, not the project overview
- Look for tabs at the top of the service page

### Variables Not Saving?
- Check for special characters in variable names
- Ensure variable names are in UPPERCASE
- Try refreshing the page

### App Not Starting?
- Check the "Logs" tab in "Deployments"
- Look for environment variable errors
- Verify all required variables are set

## 📞 Need Help?

- **Railway Docs**: [docs.railway.app](https://docs.railway.app)
- **Railway Discord**: [discord.gg/railway](https://discord.gg/railway)
- **Check Logs**: Always check logs first for error messages
