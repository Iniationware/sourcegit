#!/bin/bash

echo "======================================"
echo "Local Build Check (GitHub Actions Simulation)"
echo "======================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track overall status
BUILD_SUCCESS=true

echo "1. Checking for unnecessary using directives (IDE0005)..."
echo "---------------------------------------------------"
if dotnet build -c Release src/SourceGit.csproj -warnaserror:IDE0005 > /tmp/build_ide0005.log 2>&1; then
    echo -e "${GREEN}✅ No IDE0005 errors found${NC}"
else
    echo -e "${RED}❌ IDE0005 errors detected:${NC}"
    grep "error IDE0005" /tmp/build_ide0005.log
    BUILD_SUCCESS=false
fi
echo ""

echo "2. Running full Release build..."
echo "--------------------------------"
if dotnet build -c Release src/SourceGit.csproj > /tmp/build_release.log 2>&1; then
    echo -e "${GREEN}✅ Release build successful${NC}"
    WARNINGS=$(grep -c "warning" /tmp/build_release.log 2>/dev/null || echo "0")
    if [ "$WARNINGS" -gt "0" ]; then
        echo -e "${YELLOW}⚠️  Build has $WARNINGS warnings${NC}"
    fi
else
    echo -e "${RED}❌ Release build failed${NC}"
    grep -E "error" /tmp/build_release.log | head -10
    BUILD_SUCCESS=false
fi
echo ""

echo "3. Checking code analysis rules..."
echo "-----------------------------------"
# Check with all code analysis rules that GitHub Actions uses
if dotnet build -c Release src/SourceGit.csproj \
    -warnaserror:IDE0005 \
    -warnaserror:CS8600 \
    -warnaserror:CS8602 \
    -warnaserror:CS8604 \
    -warnaserror:CS8618 \
    -warnaserror:CS8625 \
    -p:TreatWarningsAsErrors=false \
    > /tmp/build_analysis.log 2>&1; then
    echo -e "${GREEN}✅ Code analysis passed${NC}"
else
    echo -e "${RED}❌ Code analysis failed:${NC}"
    grep -E "error" /tmp/build_analysis.log | head -10
    BUILD_SUCCESS=false
fi
echo ""

echo "4. Checking for common issues..."
echo "---------------------------------"

# Check for tabs vs spaces issues
TAB_FILES=$(find src -name "*.cs" -exec grep -l $'\t' {} \; 2>/dev/null | wc -l)
if [ "$TAB_FILES" -eq 0 ]; then
    echo -e "${GREEN}✅ No tab/space issues${NC}"
else
    echo -e "${YELLOW}⚠️  $TAB_FILES files contain tabs${NC}"
fi

# Check for BOM issues
BOM_FILES=$(find src -name "*.cs" -exec file {} \; | grep -c "with BOM" || echo "0")
if [ "$BOM_FILES" -eq 0 ]; then
    echo -e "${GREEN}✅ No BOM issues${NC}"
else
    echo -e "${YELLOW}⚠️  $BOM_FILES files have BOM${NC}"
fi

# Check for line ending issues (CRLF vs LF)
CRLF_FILES=$(find src -name "*.cs" -exec file {} \; | grep -c "CRLF" || echo "0")
echo -e "${YELLOW}ℹ️  $CRLF_FILES files use CRLF line endings${NC}"

echo ""
echo "5. Test compilation for all platforms..."
echo "-----------------------------------------"

# Test Windows build
echo -n "Windows (win-x64): "
if dotnet publish src/SourceGit.csproj -c Release -r win-x64 --self-contained -o /tmp/test-win > /dev/null 2>&1; then
    echo -e "${GREEN}✅${NC}"
else
    echo -e "${RED}❌${NC}"
    BUILD_SUCCESS=false
fi

# Test macOS build
echo -n "macOS (osx-arm64): "
if dotnet publish src/SourceGit.csproj -c Release -r osx-arm64 --self-contained -o /tmp/test-osx > /dev/null 2>&1; then
    echo -e "${GREEN}✅${NC}"
else
    echo -e "${RED}❌${NC}"
    BUILD_SUCCESS=false
fi

# Test Linux build
echo -n "Linux (linux-x64): "
if dotnet publish src/SourceGit.csproj -c Release -r linux-x64 --self-contained -o /tmp/test-linux > /dev/null 2>&1; then
    echo -e "${GREEN}✅${NC}"
else
    echo -e "${RED}❌${NC}"
    BUILD_SUCCESS=false
fi

# Cleanup temp directories
rm -rf /tmp/test-win /tmp/test-osx /tmp/test-linux 2>/dev/null

echo ""
echo "======================================"
if [ "$BUILD_SUCCESS" = true ]; then
    echo -e "${GREEN}✅ ALL CHECKS PASSED!${NC}"
    echo "Ready to commit and push to GitHub."
else
    echo -e "${RED}❌ SOME CHECKS FAILED!${NC}"
    echo "Please fix the issues before pushing to GitHub."
    echo ""
    echo "To see detailed errors, check:"
    echo "  /tmp/build_ide0005.log"
    echo "  /tmp/build_release.log"
    echo "  /tmp/build_analysis.log"
fi
echo "======================================"

# Exit with error if build failed
if [ "$BUILD_SUCCESS" = false ]; then
    exit 1
fi