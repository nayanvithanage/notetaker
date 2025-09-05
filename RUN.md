# 🚀 **Notetaker Application - Complete Run Instructions**

## **Prerequisites**

Before running the application, ensure you have the following installed:

- **.NET 8 SDK** (8.0.413 or later)
- **Node.js 20+** and npm
- **Docker Desktop** (for PostgreSQL database)
- **Visual Studio Code** or **Visual Studio** (recommended)

## **�� Step-by-Step Setup Instructions**

### **1. Clone and Navigate to Project**
```bash
cd D:\Code\fullstack\code_7
```

### **2. Start the Database**
```bash
# Start PostgreSQL and Adminer using Docker Compose
docker-compose up -d

# Verify containers are running
docker ps
```

**Expected Output:**
- `notetaker-postgres` container running on port 5432
- `notetaker-adminer` container running on port 8080

**Database Access:**
- **Adminer UI**: http://localhost:8080
- **Server**: `postgres`
- **Username**: `postgres`
- **Password**: `postgres`
- **Database**: `notetaker`

### **3. Configure API Keys**

Edit `notetaker/Notetaker.Api/appsettings.json` and add your API keys:

```json
{
  "Google": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret"
  },
  "RecallAi": {
    "ApiKey": "your-recall-ai-api-key"
  },
  "LinkedIn": {
    "ClientId": "your-linkedin-client-id",
    "ClientSecret": "your-linkedin-client-secret"
  },
  "Facebook": {
    "AppId": "your-facebook-app-id",
    "AppSecret": "your-facebook-app-secret"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

### **4. Run the Backend API**

```bash
# Navigate to API directory
cd notetaker

# Restore packages
dotnet restore

# Run the API
dotnet run --project Notetaker.Api
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

**API Endpoints:**
- **API Base URL**: https://localhost:7001/api
- **Swagger UI**: https://localhost:7001/swagger
- **Hangfire Dashboard**: https://localhost:7001/hangfire

### **5. Run the Frontend (New Terminal)**

```bash
# Navigate to frontend directory
cd notetaker-web

# Install dependencies
npm install

# Start the development server
ng serve
```

**Expected Output:**
```
✔ Browser application bundle generation complete.
Initial chunk files | Names         |  Raw size
main.js             | main          |   2.44 MB | 
polyfills.js        | polyfills     |  89.77 kB | 
styles.css          | styles        |  96 bytes | 

Local:   http://localhost:4200/
Network: http://192.168.x.x:4200/
```

**Frontend URL**: http://localhost:4200

## **🔧 Configuration Details**

### **Google OAuth Setup**
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URI: `https://localhost:7001/api/auth/google/callback`
6. Copy Client ID and Secret to `appsettings.json`

### **Recall.ai Setup**
1. Sign up at [Recall.ai](https://recall.ai/)
2. Get your API key from the dashboard
3. Add to `appsettings.json`

### **OpenAI Setup**
1. Get API key from [OpenAI Platform](https://platform.openai.com/)
2. Add to `appsettings.json`

### **LinkedIn Setup**
1. Create app at [LinkedIn Developers](https://www.linkedin.com/developers/)
2. Add redirect URI: `https://localhost:7001/api/auth/social/callback`
3. Add Client ID and Secret to `appsettings.json`

### **Facebook Setup**
1. Create app at [Facebook Developers](https://developers.facebook.com/)
2. Add redirect URI: `https://localhost:7001/api/auth/social/callback`
3. Add App ID and Secret to `appsettings.json`

## **🎯 Application Features**

### **Backend Features**
- ✅ **Authentication**: Google OAuth + JWT tokens
- ✅ **Calendar Integration**: Google Calendar event polling
- ✅ **Recall.ai Integration**: Bot scheduling and transcript fetching
- ✅ **AI Content Generation**: OpenAI integration
- ✅ **Social Media**: LinkedIn and Facebook posting
- ✅ **Background Jobs**: Hangfire for automated processes
- ✅ **Database**: PostgreSQL with EF Core

### **Frontend Features**
- ✅ **Material Design**: Modern Angular UI
- ✅ **Meetings Management**: View and manage meetings
- ✅ **Notetaker Toggle**: Enable/disable for meetings
- ✅ **Content Generation**: AI-powered social media posts
- ✅ **Settings**: Configure automations and connections

## **🔍 Troubleshooting**

### **Common Issues**

**1. Database Connection Issues**
```bash
# Check if PostgreSQL is running
docker ps

# Restart if needed
docker-compose down
docker-compose up -d
```

**2. API Not Starting**
```bash
# Check if port 7001 is available
netstat -an | findstr :7001

# Try different port
dotnet run --project Notetaker.Api --urls="https://localhost:7002"
```

**3. Frontend Build Issues**
```bash
# Clear node modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Clear Angular cache
ng cache clean
```

**4. CORS Issues**
- Ensure frontend URL is `http://localhost:4200`
- Check CORS configuration in `Program.cs`

### **Logs and Debugging**

**API Logs:**
- Check console output for detailed logs
- Logs are also written to `logs/notetaker-*.txt`

**Frontend Logs:**
- Check browser console (F12)
- Angular dev tools available

**Database Logs:**
```bash
# View PostgreSQL logs
docker logs notetaker-postgres
```

## **📱 Using the Application**

### **1. First Time Setup**
1. Open http://localhost:4200
2. Click "Login" to authenticate with Google
3. Complete OAuth flow
4. You'll be redirected back to the app

### **2. Connect Calendar**
1. Go to Settings page
2. Connect Google Calendar
3. Authorize calendar access

### **3. Configure Automations**
1. Go to Automations page
2. Create new automation for LinkedIn/Facebook
3. Set up prompts and examples

### **4. Manage Meetings**
1. Go to Meetings page
2. View upcoming meetings
3. Toggle notetaker on/off
4. View past meetings and generated content

## **🛠️ Development Commands**

### **Backend Commands**
```bash
# Run with hot reload
dotnet watch run --project Notetaker.Api

# Run migrations
dotnet ef migrations add Initial --project Notetaker.Api
dotnet ef database update --project Notetaker.Api

# Build for production
dotnet build --configuration Release
```

### **Frontend Commands**
```bash
# Development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test

# Lint code
ng lint
```

### **Database Commands**
```bash
# Start database
docker-compose up -d

# Stop database
docker-compose down

# View logs
docker-compose logs -f postgres
```

## **🌐 Production Deployment**

### **Environment Variables**
Set these environment variables in production:

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=your-production-connection-string
JWT__SigningKey=your-production-jwt-key
GOOGLE__ClientId=your-production-google-client-id
GOOGLE__ClientSecret=your-production-google-client-secret
# ... other API keys
```

### **Build Commands**
```bash
# Build API
dotnet publish Notetaker.Api -c Release -o ./publish

# Build Frontend
ng build --configuration production
```

## **✅ Verification Checklist**

- [ ] PostgreSQL database running on port 5432
- [ ] API running on https://localhost:7001
- [ ] Frontend running on http://localhost:4200
- [ ] Swagger UI accessible at https://localhost:7001/swagger
- [ ] Hangfire dashboard accessible at https://localhost:7001/hangfire
- [ ] Adminer accessible at http://localhost:8080
- [ ] All API keys configured in appsettings.json
- [ ] Google OAuth working (can login)
- [ ] Meetings page loads without errors

## **📞 Support**

If you encounter any issues:

1. **Check the logs** (API console, browser console, Docker logs)
2. **Verify all prerequisites** are installed
3. **Ensure all API keys** are correctly configured
4. **Check port availability** (7001, 4200, 5432, 8080)
5. **Restart services** if needed

