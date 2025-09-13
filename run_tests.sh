#!/bin/bash
# Test runner script for SourceGit

echo "========================================"
echo "SourceGit Test Suite"
echo "========================================"
echo ""

# Build in Debug mode
echo "Building Debug configuration..."
dotnet build -c Debug --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Debug build failed!"
    exit 1
fi
echo "✅ Debug build succeeded"
echo ""

# Run tests in Debug mode
echo "Running tests (Debug)..."
dotnet test -c Debug --no-build --verbosity minimal
if [ $? -ne 0 ]; then
    echo "❌ Debug tests failed!"
    exit 1
fi
echo ""

# Build in Release mode
echo "Building Release configuration..."
dotnet build -c Release --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Release build failed!"
    exit 1
fi
echo "✅ Release build succeeded"
echo ""

# Run tests in Release mode
echo "Running tests (Release)..."
dotnet test -c Release --no-build --verbosity minimal
if [ $? -ne 0 ]; then
    echo "❌ Release tests failed!"
    exit 1
fi
echo ""

echo "========================================"
echo "✅ All builds and tests passed successfully!"
echo "========================================"