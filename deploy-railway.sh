#!/bin/bash

# Railway Deployment Script for Notetaker
echo "🚀 Starting Railway deployment for Notetaker..."

# Check if Railway CLI is installed
if ! command -v railway &> /dev/null; then
    echo "❌ Railway CLI not found. Please install it first:"
    echo "   npm install -g @railway/cli"
    echo "   or visit: https://docs.railway.app/develop/cli"
    exit 1
fi

# Check if user is logged in
if ! railway whoami &> /dev/null; then
    echo "🔐 Please log in to Railway first:"
    echo "   railway login"
    exit 1
fi

echo "✅ Railway CLI is ready"

# Create or link to Railway project
echo "🔗 Linking to Railway project..."
if [ -f ".railway/project.json" ]; then
    echo "   Project already linked"
else
    echo "   Please run: railway link"
    echo "   Then select your project or create a new one"
    exit 1
fi

# Set up environment variables
echo "🔧 Setting up environment variables..."
echo "   Please set the following environment variables in Railway dashboard:"
echo ""
echo "   Database:"
echo "   - DATABASE_HOST"
echo "   - DATABASE_NAME"
echo "   - DATABASE_USER"
echo "   - DATABASE_PASSWORD"
echo "   - DATABASE_PORT"
echo ""
echo "   JWT:"
echo "   - JWT_SIGNING_KEY"
echo ""
echo "   Google OAuth:"
echo "   - GOOGLE_CLIENT_ID"
echo "   - GOOGLE_CLIENT_SECRET"
echo ""
echo "   External APIs:"
echo "   - RECALL_AI_API_KEY"
echo "   - OPENAI_API_KEY"
echo "   - LINKEDIN_CLIENT_ID"
echo "   - LINKEDIN_CLIENT_SECRET"
echo "   - FACEBOOK_APP_ID"
echo "   - FACEBOOK_APP_SECRET"
echo ""

# Deploy the application
echo "🚀 Deploying to Railway..."
railway up

echo "✅ Deployment initiated!"
echo "📊 Monitor your deployment at: https://railway.app"
echo "🔍 Check logs with: railway logs"
echo "🌐 Your app will be available at the URL shown in the Railway dashboard"
