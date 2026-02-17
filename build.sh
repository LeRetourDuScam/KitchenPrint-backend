#!/bin/bash
# Build script for deployment

echo "Restoring NuGet packages..."
dotnet restore tibg-sport-backend/tibg-sport-backend.sln

echo "Building application..."
dotnet build tibg-sport-backend/tibg-sport-backend.sln -c Release

echo "Publishing application..."
dotnet publish tibg-sport-backend/tibg-sport-backend.csproj -c Release -o ./publish

echo "Build complete!"
