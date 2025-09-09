# Multi-stage build for .NET API and Angular frontend
FROM node:20-alpine AS frontend-builder

# Set working directory for frontend
WORKDIR /app/frontend

# Copy frontend package files
COPY notetaker/notetaker-web/package*.json ./

# Install frontend dependencies (including dev dependencies for build)
RUN npm ci

# Copy frontend source code
COPY notetaker/notetaker-web/ ./

# Build frontend for production
RUN npx ng build --configuration=railway

# .NET build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-builder

# Set working directory for API
WORKDIR /app/api

# Copy API project file
COPY notetaker/Notetaker.Api/Notetaker.Api.csproj ./

# Restore API dependencies
RUN dotnet restore

# Copy API source code
COPY notetaker/Notetaker.Api/ ./

# Build API
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install Node.js for serving static files (optional)
RUN apt-get update && apt-get install -y nodejs npm && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy API from build stage
COPY --from=api-builder /app/publish .

# Copy frontend build from frontend stage
COPY --from=frontend-builder /app/frontend/dist/notetaker-web ./wwwroot

# Create logs directory
RUN mkdir -p logs

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "Notetaker.Api.dll"]
