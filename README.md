# Notetaker v1.0.0

## 📋 Overview

Notetaker is a comprehensive meeting management and AI-powered content generation application that integrates with Recall.ai for automated meeting transcription and analysis. The application provides intelligent meeting summaries, social media post generation, and follow-up email automation.

## 🏗️ Architecture

### Backend (.NET API)
- **Framework**: .NET 8.0 (C# 12)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT-based with Google OAuth integration
- **External APIs**: Recall.ai, Google Calendar, LinkedIn, Facebook, OpenAI
- **Background Jobs**: Hangfire with PostgreSQL storage
- **Logging**: Serilog with console and file sinks

### Frontend (Angular)
- **Framework**: Angular 20 with TypeScript 5.5+
- **UI Library**: Angular Material 20.x
- **State Management**: RxJS Observables and Signals
- **Styling**: SCSS with responsive design
- **Authentication**: angular-oauth2-oidc with PKCE

## 🚀 Environment Setup

### Prerequisites
- .NET 8.0 SDK (8.0.408 LTS or later)
- Node.js 20+ and npm 10+
- Docker Desktop (for PostgreSQL)
- Visual Studio 2022 or VS Code
- Git

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd notetaker
   ```

2. **Start the database**
   ```bash
   # Start PostgreSQL and Adminer using Docker Compose
   docker-compose up -d
   
   # Verify containers are running
   docker ps
   ```

3. **Configure API keys**
   ```bash
   # Copy configuration template
   cp Notetaker.Api/appsettings.template.json Notetaker.Api/appsettings.json
   ```

4. **Update `appsettings.json` with your credentials:**
   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=localhost;Database=notetaker;Username=postgres;Password=postgres"
     },
     "Jwt": {
       "Issuer": "Notetaker",
       "Audience": "Notetaker-Users",
       "SigningKey": "your-super-secret-jwt-key-at-least-32-characters-long"
     },
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

5. **Install dependencies and run**
   ```bash
   cd Notetaker.Api
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

   **API will be available at:**
   - **API Base URL**: http://localhost:5135/api
   - **Swagger UI**: http://localhost:5135/swagger
   - **Hangfire Dashboard**: http://localhost:5135/hangfire

### Frontend Setup

1. **Navigate to frontend directory**
   ```bash
   cd notetaker-web
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Configure environment**
   ```bash
   # Copy environment template
   cp src/environments/environment.template.ts src/environments/environment.ts
   ```

4. **Update `environment.ts` with your configuration:**
   ```typescript
   export const environment = {
     production: false,
     apiBaseUrl: 'http://localhost:5135/api',
     googleClientId: 'your-google-client-id'
   };
   ```

5. **Run the development server**
   ```bash
   ng serve
   ```

   **Application will be available at:**
   - **Frontend URL**: http://localhost:4200

## 🔄 First-Time Setup & Data Syncing

After cloning the repository and setting up the environment, you need to sync data to populate the application with meeting and bot information. Follow these steps to get your Notetaker application fully functional:

### Step 1: Initial Data Sync

1. **Start both backend and frontend applications** (as described above)

2. **Access the application** at http://localhost:4200

3. **Login with Google OAuth** to authenticate your account

4. **Navigate to Settings page** (gear icon in the sidebar)

### Step 2: Sync Calendar Events

1. **Connect Google Calendar**:
   - In Settings, find the "Google Calendar Integration" section
   - Click "Connect Google Calendar"
   - Complete the OAuth flow to grant calendar access
   - This will sync your calendar events to the database

2. **Sync Calendar Events**:
   - Go to the "Meetings" page
   - Click the "Sync Calendar" button
   - This will download all calendar events from your Google Calendar
   - Wait for the sync to complete (you'll see a success message)

### Step 3: Sync Recall.ai Bots

1. **Sync All Bots**:
   - In Settings, find the "Bot Management" section
   - Click "Sync All Bots" button
   - This will download all your Recall.ai bots and their data
   - Wait for the sync to complete

2. **Verify Bot Data**:
   - Check the "Bot Statistics" section to see how many bots were synced
   - You should see bot counts and status information

### Step 4: Create Meeting Records

1. **Create Missing Meeting Records**:
   - In the Meetings page, click "Sync Calendar" again
   - This will now create meeting records for calendar events that have associated bots
   - The system will automatically match bots to calendar events based on meeting URLs

2. **Verify Meeting-Bot Associations**:
   - Check your meetings list
   - You should now see bot IDs displayed for meetings that have associated bots
   - The "Notetaker" toggle should be enabled for meetings with bots

### Step 5: Test the Complete Flow

1. **View Meeting Details**:
   - Click on any meeting with bot associations
   - Verify that bot details are displayed correctly
   - Check that transcripts and recordings are accessible

2. **Test Content Generation**:
   - For past meetings, try generating content
   - Test social media post creation
   - Verify follow-up email generation

### Troubleshooting Data Sync Issues

#### If Calendar Events Don't Appear:
```bash
# Check API logs for errors
tail -f Notetaker.Api/logs/notetaker-*.txt

# Verify Google Calendar API credentials
# Check that OAuth scopes include calendar access
```

#### If Bot Data is Missing:
```bash
# Test Recall.ai API connection
curl -H "Authorization: Token YOUR_RECALL_AI_API_KEY" \
     https://us-west-2.recall.ai/api/v1/bot/

# Check bot sync logs in the API console
```

#### If Meeting-Bot Associations Don't Work:
- Verify that bot `MeetingId` fields match calendar event `JoinUrl` patterns
- Check that the dynamic meeting creation process ran successfully
- Look for "Meeting record synchronization completed" in the logs

### Data Recovery After Deployment

If you deploy to a new environment and lose meeting data:

1. **The system will automatically recreate meeting records** when you access the Meetings page
2. **No manual intervention required** - the dynamic meeting creation process handles this
3. **All bot associations will be restored** based on existing calendar events and bot data

### Expected Data Flow

```
Google Calendar → Calendar Events → Meeting Records ← Recall.ai Bots
     ↓                ↓                    ↓              ↓
  OAuth Sync    Database Storage    Junction Table    Bot Sync
     ↓                ↓                    ↓              ↓
  User Login    Meetings Page    Bot Associations    Bot Details
```

### Database Access
- **Adminer UI**: http://localhost:8080
- **Server**: postgres
- **Username**: postgres
- **Password**: postgres
- **Database**: notetaker

## 🔧 Core Functionalities

### 1. Meeting Management
- **Calendar Integration**: Sync with Google Calendar
- **Meeting Creation**: Automated meeting setup with Recall.ai bots
- **Meeting Details**: Comprehensive meeting information display
- **Delta Sync**: Synchronize existing meetings with Recall.ai data
- **Platform Detection**: Automatic detection of Zoom, Teams, Meet, and other platforms

### 2. AI-Powered Transcription
- **Automated Recording**: Recall.ai bot integration for meeting recording
- **Real-time Transcription**: Live transcription during meetings
- **Transcript Management**: Fetch, store, and display meeting transcripts
- **Smart Bot Selection**: Prioritize bots with available transcripts
- **Background Polling**: Automated status checking and transcript fetching

### 3. Content Generation
- **Meeting Summaries**: AI-generated meeting summaries using OpenAI
- **Action Items**: Automated extraction of action items
- **Social Media Posts**: Platform-specific post generation
- **Follow-up Emails**: Automated email generation for meeting participants
- **Custom Automations**: User-defined content generation prompts

### 4. Social Media Integration
- **Multi-Platform Support**: LinkedIn, Twitter, Facebook, Instagram
- **OAuth Integration**: Secure authentication with social platforms
- **Automated Posting**: Schedule and publish social media content
- **Engagement Tracking**: Monitor likes, comments, and shares
- **Content Templates**: Pre-configured templates for different platforms
- **Page Management**: Facebook page selection and management

### 5. User Management
- **Google OAuth**: Secure authentication with Google accounts
- **JWT Tokens**: Access and refresh token management
- **User Profiles**: Gmail integration with user information
- **Session Management**: HttpOnly refresh cookies for security

## 📊 Database Schema

### Core Tables
- **users**: User authentication and profile data
- **user_tokens**: Encrypted external service tokens
- **google_calendar_accounts**: Google Calendar sync configuration
- **calendar_events**: Meeting calendar entries with platform detection
- **meetings**: Meeting details with Recall.ai integration
- **meeting_transcripts**: Stored transcript data and summaries
- **automations**: AI automation configurations per platform
- **generated_contents**: AI-generated content history
- **social_accounts**: Connected social media accounts
- **social_posts**: Generated social media content and status

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/google/start` - Initiate Google OAuth flow
- `GET /api/auth/google/callback` - Handle OAuth callback
- `POST /api/auth/refresh` - Refresh JWT tokens
- `POST /api/auth/logout` - User logout

### Calendar Integration
- `POST /api/google/connect` - OAuth grant for Calendar access
- `GET /api/calendar/events` - Get calendar events
- `POST /api/calendar/events/{id}/notetaker:toggle` - Enable/disable notetaker

### Meetings
- `GET /api/meetings` - List all meetings
- `GET /api/meetings/{id}` - Get meeting details
- `POST /api/meetings/sync` - Sync meetings with Recall.ai
- `POST /api/meetings/transcript:fetch-by-bot` - Fetch transcript by bot ID

### Content Generation
- `POST /api/meetings/{id}/generate` - Generate meeting content
- `GET /api/automations` - Get user automations
- `POST /api/automations` - Create new automation

### Social Media
- `POST /api/linkedin/connect` - Connect LinkedIn account
- `POST /api/facebook/connect` - Connect Facebook account
- `GET /api/facebook/pages` - Get Facebook pages
- `POST /api/social/post` - Post to social media
- `GET /api/social/posts` - Get social media posts

## 🎨 Frontend Components

### Main Layout
- **Navigation**: Sidebar with meetings, automations, and settings
- **User Menu**: Gmail user display with logout functionality
- **Responsive Design**: Mobile-friendly interface

### Meeting Management
- **Meetings List**: Calendar view with meeting cards
- **Meeting Details**: Comprehensive meeting information
- **Transcript Display**: Formatted transcript with actions
- **Content Generation**: AI-powered content creation
- **Notetaker Toggle**: Enable/disable for individual meetings

### Content Generation
- **Automation Selection**: Choose from pre-configured automations
- **Custom Prompts**: Create custom content generation prompts
- **Social Media Posts**: Multi-platform post management
- **Follow-up Emails**: Automated email generation

### Settings
- **Social Connections**: Manage LinkedIn and Facebook accounts
- **Automation Management**: Create and configure content automations
- **Bot Configuration**: Set lead time for bot scheduling

## 🔧 Configuration

### Backend Configuration
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=notetaker;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "Notetaker",
    "Audience": "Notetaker-Users",
    "SigningKey": "your-super-secret-jwt-key"
  },
  "Google": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret"
  },
  "RecallAi": {
    "ApiKey": "your-recall-ai-api-key"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

### Frontend Configuration
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5135/api',
  googleClientId: 'your-google-client-id'
};
```

## 🚀 Deployment

### Backend Deployment
1. **Build the application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Configure production settings**
   - Update connection strings for production database
   - Set production API keys via environment variables
   - Configure CORS for frontend domain

3. **Deploy to hosting platform**
   - Azure App Service
   - AWS Elastic Beanstalk
   - Docker container

### Frontend Deployment
1. **Build for production**
   ```bash
   ng build --configuration production
   ```

2. **Deploy to hosting platform**
   - Azure Static Web Apps
   - AWS S3 + CloudFront
   - Netlify
   - Vercel

## 🧪 Testing

### Backend Testing
```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration
```

### Frontend Testing
```bash
# Run unit tests
ng test

# Run e2e tests
ng e2e
```

## 📝 Development Features

### Mock Data
- **Gmail Users**: 5 pre-configured Gmail test users
- **Social Posts**: Sample social media content
- **Follow-up Emails**: Mock email templates
- **Meeting Data**: Sample meeting information

### Console Testing
```javascript
// Switch between Gmail users
switchToGmailUser('john')     // john.doe@gmail.com
switchToGmailUser('sarah')    // sarah.johnson@gmail.com
switchToGmailUser('alex')     // alex.chen@gmail.com
switchToGmailUser('maria')    // maria.garcia@gmail.com
switchToGmailUser('david')    // david.wilson@gmail.com

// Set custom user name
setUserName("Custom Name")
```

## 🔒 Security Features

- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Google OAuth**: Industry-standard OAuth 2.0 with PKCE
- **Token Encryption**: External service tokens encrypted at rest
- **CORS Configuration**: Secure cross-origin requests
- **Input Validation**: Server-side validation for all inputs
- **SQL Injection Protection**: Entity Framework parameterized queries
- **HttpOnly Cookies**: Secure refresh token storage

## 📈 Performance Optimizations

- **Lazy Loading**: Angular lazy-loaded modules
- **Caching**: HTTP response caching
- **Database Indexing**: Optimized database queries
- **Background Jobs**: Asynchronous processing with Hangfire
- **CDN Integration**: Static asset optimization
- **Connection Pooling**: Efficient database connections

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify PostgreSQL is running: `docker ps`
   - Check connection string in `appsettings.json`
   - Run `dotnet ef database update`

2. **Recall.ai API Issues**
   - Verify API key configuration
   - Check network connectivity
   - Review API rate limits

3. **Frontend Build Issues**
   - Clear node_modules and reinstall
   - Check Angular version compatibility
   - Verify environment configuration

4. **CORS Issues**
   - Ensure frontend URL is `http://localhost:4200`
   - Check CORS configuration in `Program.cs`

### Logs and Debugging

**API Logs:**
- Check console output for detailed logs
- Logs written to `logs/notetaker-*.txt`

**Frontend Logs:**
- Check browser console (F12)
- Angular dev tools available

**Database Logs:**
```bash
# View PostgreSQL logs
docker logs notetaker-postgres
```

## 📚 API Documentation

### Swagger/OpenAPI
- Available at: `http://localhost:5135/swagger`
- Interactive API documentation
- Request/response examples
- Authentication testing

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

**Version**: 1.0.0  
**Last Updated**: January 2025  
**Maintainer**: Notetaker Development Team
