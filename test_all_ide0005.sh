#!/bin/bash

echo "=========================================="
echo "Complete IDE0005 Check (GitHub Actions Mode)"
echo "=========================================="
echo ""

# Use exact same build command as GitHub Actions
echo "Running build with strict IDE0005 checking..."
echo "Building with: -c Release -warnaserror"
echo ""

# First, clean everything
dotnet clean src/SourceGit.csproj > /dev/null 2>&1

# Build with exact GitHub Actions settings
if dotnet build src/SourceGit.csproj \
    -c Release \
    -warnaserror \
    --no-restore \
    2>&1 | tee /tmp/build_full.log | grep "error IDE0005"; then
    echo ""
    echo "❌ FOUND IDE0005 ERRORS!"
    echo ""
    echo "Affected files:"
    grep "error IDE0005" /tmp/build_full.log | cut -d'(' -f1 | sort -u
    exit 1
else
    echo "✅ No IDE0005 errors found!"
fi

# Also check with EnforceCodeStyleInBuild
echo ""
echo "Checking with EnforceCodeStyleInBuild=true..."
if dotnet build src/SourceGit.csproj \
    -c Release \
    -p:EnforceCodeStyleInBuild=true \
    -warnaserror:IDE0005 \
    2>&1 | grep "error IDE0005"; then
    echo ""
    echo "❌ Found additional IDE0005 errors with strict code style!"
    exit 1
else
    echo "✅ No additional IDE0005 errors!"
fi

echo ""
echo "=========================================="
echo "✅ ALL CHECKS PASSED - Ready for GitHub!"
echo "=========================================="