# Notetaker Application Setup

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- PostgreSQL
- Docker (optional)

## Quick Start

### 1. Clone the Repository
```bash
git clone <your-repo-url>
cd notetaker
```

### 2. Backend Setup (.NET API)

1. **Copy the configuration template:**
   ```bash
   cp Notetaker.Api/appsettings.template.json Notetaker.Api/appsettings.json
   ```

2. **Update `appsettings.json` with your credentials:**
   - Replace `YOUR_POSTGRES_PASSWORD` with your PostgreSQL password
   - Replace `YOUR_SUPER_SECRET_JWT_KEY_AT_LEAST_32_CHARACTERS_LONG` with a secure JWT key
   - Replace `YOUR_GOOGLE_CLIENT_ID` and `YOUR_GOOGLE_CLIENT_SECRET` with your Google OAuth credentials
   - Replace other API keys as needed

3. **Install dependencies and run:**
   ```bash
   cd Notetaker.Api
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

### 3. Frontend Setup (Angular)

1. **Copy the environment template:**
   ```bash
   cp notetaker-web/src/environments/environment.template.ts notetaker-web/src/environments/environment.ts
   ```

2. **Update `environment.ts` with your Google Client ID:**
   ```typescript
   export const environment = {
     production: false,
     apiBaseUrl: 'http://localhost:5135/api',
     googleClientId: 'YOUR_GOOGLE_CLIENT_ID'
   };
   ```

3. **Install dependencies and run:**
   ```bash
   cd notetaker-web
   npm install
   ng serve
   ```

### 4. Database Setup

**Option A: Using Docker (Recommended)**
```bash
docker-compose up -d
```

**Option B: Manual PostgreSQL Setup**
1. Install PostgreSQL
2. Create database: `notetaker`
3. Update connection string in `appsettings.json`

## Required API Keys

### Google OAuth
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API and Google Calendar API
4. Create OAuth 2.0 credentials
5. Add redirect URIs:
   - `http://localhost:5135/api/auth/google/callback`
   - `http://localhost:4200/auth/callback`

### Other APIs (Optional)
- **Recall.ai**: For meeting transcription
- **OpenAI**: For content generation
- **LinkedIn**: For social posting
- **Facebook**: For social posting

## Environment Variables

For production deployment, set these environment variables:

```bash
# Database
CONNECTION_STRING="Host=your-host;Database=notetaker;Username=your-user;Password=your-password"

# JWT
JWT_SIGNING_KEY="your-super-secret-jwt-key"

# Google OAuth
GOOGLE_CLIENT_ID="your-google-client-id"
GOOGLE_CLIENT_SECRET="your-google-client-secret"

# Other APIs
RECALL_AI_API_KEY="your-recall-ai-key"
OPENAI_API_KEY="your-openai-key"
```

## Development

### Running the Application
1. Start PostgreSQL (via Docker or local installation)
2. Start the .NET API: `dotnet run --project Notetaker.Api`
3. Start Angular: `ng serve --project notetaker-web`
4. Visit: `http://localhost:4200`

### Database Migrations
```bash
cd Notetaker.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Security Notes

- Never commit `appsettings.json` or `environment.ts` with real credentials
- Use environment variables for production
- Rotate API keys regularly
- Use strong, unique passwords for all services

## Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Check PostgreSQL is running
   - Verify connection string in `appsettings.json`

2. **Google OAuth Error**
   - Verify redirect URIs in Google Cloud Console
   - Check client ID and secret

3. **Angular Build Error**
   - Run `npm install` to ensure all dependencies are installed
   - Check Node.js version compatibility

## Support

For issues and questions, please create an issue in the repository.
