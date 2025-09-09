#!/bin/bash

# Build script for Railway deployment
echo "Building Notetaker application for Railway..."

# Build Angular frontend
echo "Building Angular frontend..."
cd notetaker/notetaker-web
npm ci
npm run build:railway
cd ../..

# Build .NET API
echo "Building .NET API..."
cd notetaker/Notetaker.Api
dotnet restore
dotnet build -c Release
cd ../..

echo "Build completed successfully!"
