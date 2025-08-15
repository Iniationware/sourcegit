#!/bin/bash

echo "Starting SourceGit Debug on macOS..."

# Clear any quarantine attributes
xattr -cr src/bin/Debug/net9.0/ 2>/dev/null

# Set environment variables
export DOTNET_ENVIRONMENT=Development
export AVALONIA_DIAGNOSTICS_ENABLE=1

# Build in Debug mode
echo "Building Debug configuration..."
dotnet build src/SourceGit.csproj -c Debug

# Run with debugging enabled
echo "Starting application..."
cd src
dotnet run --configuration Debug

# Alternative: Run the binary directly
# ./bin/Debug/net9.0/SourceGit