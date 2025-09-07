# Notetaker v1.0.0

## 📋 Overview

Notetaker is a comprehensive meeting management and AI-powered content generation application that integrates with Recall.ai for automated meeting transcription and analysis. The application provides intelligent meeting summaries, social media post generation, and follow-up email automation.

## 🏗️ Architecture

### Backend (.NET API)
- **Framework**: .NET 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT-based with Google OAuth integration
- **External APIs**: Recall.ai integration for meeting transcription
- **Background Jobs**: Hangfire for automated meeting processing

### Frontend (Angular)
- **Framework**: Angular 17
- **UI Library**: Angular Material
- **State Management**: RxJS Observables
- **Styling**: SCSS with responsive design

## 🚀 Environment Setup

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code
- Git

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd notetaker/Notetaker.Api
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Database setup**
   ```bash
   # Update connection string in appsettings.json
   dotnet ef database update
   ```

4. **Configure Recall.ai API**
   ```json
   // appsettings.json
   {
     "RecallAi": {
       "ApiKey": "your-recall-ai-api-key",
       "BaseUrl": "https://us-west-2.recall.ai/api/v1"
     }
   }
   ```

5. **Run the API**
   ```bash
   dotnet run
   ```
   - API will be available at: `http://localhost:5135`

### Frontend Setup

1. **Navigate to frontend directory**
   ```bash
   cd notetaker/notetaker-web
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Configure API endpoint**
   ```typescript
   // src/environments/environment.ts
   export const environment = {
     production: false,
     apiUrl: 'http://localhost:5135/api'
   };
   ```

4. **Run the development server**
   ```bash
   ng serve
   ```
   - Application will be available at: `http://localhost:4200`

## 🔧 Core Functionalities

### 1. Meeting Management
- **Calendar Integration**: Sync with Google Calendar
- **Meeting Creation**: Automated meeting setup with Recall.ai bots
- **Meeting Details**: Comprehensive meeting information display
- **Delta Sync**: Synchronize existing meetings with Recall.ai data

### 2. AI-Powered Transcription
- **Automated Recording**: Recall.ai bot integration for meeting recording
- **Real-time Transcription**: Live transcription during meetings
- **Transcript Management**: Fetch, store, and display meeting transcripts
- **Smart Bot Selection**: Prioritize bots with available transcripts

### 3. Content Generation
- **Meeting Summaries**: AI-generated meeting summaries
- **Action Items**: Automated extraction of action items
- **Social Media Posts**: Platform-specific post generation
- **Follow-up Emails**: Automated email generation for meeting participants

### 4. Social Media Integration
- **Multi-Platform Support**: LinkedIn, Twitter, Facebook, Instagram
- **Automated Posting**: Schedule and publish social media content
- **Engagement Tracking**: Monitor likes, comments, and shares
- **Content Templates**: Pre-configured templates for different platforms

### 5. User Management
- **Google OAuth**: Secure authentication with Google accounts
- **User Profiles**: Gmail integration with user information
- **Session Management**: JWT-based session handling

## 📊 Database Schema

### Core Tables
- **CalendarEvents**: Meeting calendar entries
- **Meetings**: Meeting details with Recall.ai integration
- **MeetingTranscripts**: Stored transcript data
- **Users**: User authentication and profile data
- **SocialPosts**: Generated social media content
- **Automations**: AI automation configurations

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/google/start` - Initiate Google OAuth
- `POST /api/auth/google/callback` - Handle OAuth callback
- `POST /api/auth/refresh` - Refresh JWT tokens
- `POST /api/auth/logout` - User logout

### Meetings
- `GET /api/meetings` - List all meetings
- `GET /api/meetings/{id}` - Get meeting details
- `POST /api/meetings/sync` - Sync meetings with Recall.ai
- `POST /api/meetings/transcript:fetch-by-bot` - Fetch transcript by bot ID

### Content Generation
- `POST /api/meetings/{id}/generate` - Generate meeting content
- `GET /api/meetings/{id}/social-posts` - Get social media posts
- `POST /api/meetings/{id}/social-posts` - Create social media post

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

### Content Generation
- **Automation Selection**: Choose from pre-configured automations
- **Custom Prompts**: Create custom content generation prompts
- **Social Media Posts**: Multi-platform post management
- **Follow-up Emails**: Automated email generation

## 🔧 Configuration

### Backend Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NotetakerDb;Trusted_Connection=true;"
  },
  "RecallAi": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://us-west-2.recall.ai/api/v1"
  },
  "Jwt": {
    "SecretKey": "your-secret-key",
    "Issuer": "Notetaker",
    "Audience": "Notetaker-Users"
  }
}
```

### Frontend Configuration
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5135/api',
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
   - Update connection strings
   - Set production API keys
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

- **JWT Authentication**: Secure token-based authentication
- **Google OAuth**: Industry-standard OAuth 2.0
- **CORS Configuration**: Secure cross-origin requests
- **Input Validation**: Server-side validation for all inputs
- **SQL Injection Protection**: Entity Framework parameterized queries

## 📈 Performance Optimizations

- **Lazy Loading**: Angular lazy-loaded modules
- **Caching**: HTTP response caching
- **Database Indexing**: Optimized database queries
- **Background Jobs**: Asynchronous processing
- **CDN Integration**: Static asset optimization

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify connection string
   - Check SQL Server service status
   - Run `dotnet ef database update`

2. **Recall.ai API Issues**
   - Verify API key configuration
   - Check network connectivity
   - Review API rate limits

3. **Frontend Build Issues**
   - Clear node_modules and reinstall
   - Check Angular version compatibility
   - Verify environment configuration

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
