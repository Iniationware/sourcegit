#!/bin/bash

# Script to check code formatting and other requirements before creating a tag
# Usage: ./check_before_tag.sh

echo "🔍 Pre-tag checks starting..."
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track if all checks pass
ALL_CHECKS_PASS=true

# 1. Check for uncommitted changes
echo "1. Checking for uncommitted changes..."
if ! git diff --quiet || ! git diff --cached --quiet; then
    echo -e "${RED}❌ Uncommitted changes detected. Please commit or stash them first.${NC}"
    ALL_CHECKS_PASS=false
else
    echo -e "${GREEN}✅ Working directory clean${NC}"
fi
echo ""

# 2. Check code formatting
echo "2. Checking code formatting..."
if dotnet format --verify-no-changes > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Code formatting is correct${NC}"
else
    echo -e "${YELLOW}⚠️  Code formatting issues detected${NC}"
    echo "   Run 'dotnet format' to fix formatting issues"
    ALL_CHECKS_PASS=false
fi
echo ""

# 3. Build the project
echo "3. Building the project..."
if dotnet build -c Release --nologo --verbosity quiet > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Build successful${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    echo "   Run 'dotnet build -c Release' to see errors"
    ALL_CHECKS_PASS=false
fi
echo ""

# 4. Run tests (if they exist)
echo "4. Checking for tests..."
if [ -d "tests" ] || find . -name "*.Tests.csproj" -o -name "*.Test.csproj" 2>/dev/null | grep -q .; then
    echo "   Running tests..."
    if dotnet test --nologo --verbosity quiet > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Tests passed${NC}"
    else
        echo -e "${RED}❌ Tests failed${NC}"
        echo "   Run 'dotnet test' to see failures"
        ALL_CHECKS_PASS=false
    fi
else
    echo -e "${YELLOW}ℹ️  No test projects found${NC}"
fi
echo ""

# 5. Check current branch
echo "5. Checking current branch..."
CURRENT_BRANCH=$(git branch --show-current)
if [[ "$CURRENT_BRANCH" == "develop" ]] || [[ "$CURRENT_BRANCH" == "master" ]] || [[ "$CURRENT_BRANCH" == "main" ]]; then
    echo -e "${GREEN}✅ On branch: $CURRENT_BRANCH${NC}"
else
    echo -e "${YELLOW}⚠️  On feature branch: $CURRENT_BRANCH${NC}"
    echo "   Consider switching to develop or master branch"
fi
echo ""

# 6. Check if up to date with remote
echo "6. Checking if up to date with remote..."
git fetch origin --quiet
LOCAL=$(git rev-parse HEAD)
REMOTE=$(git rev-parse origin/$CURRENT_BRANCH 2>/dev/null)
if [ "$LOCAL" = "$REMOTE" ]; then
    echo -e "${GREEN}✅ Branch is up to date with origin${NC}"
elif [ -z "$REMOTE" ]; then
    echo -e "${YELLOW}⚠️  No remote tracking branch${NC}"
else
    echo -e "${YELLOW}⚠️  Branch differs from origin${NC}"
    echo "   Consider pulling latest changes or pushing local commits"
fi
echo ""

# Final summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ "$ALL_CHECKS_PASS" = true ]; then
    echo -e "${GREEN}✅ All checks passed! Ready to create tag.${NC}"
    echo ""
    echo "To create a new tag, run:"
    echo "  git tag -a v2025.34-IW.X -m \"Release description\""
    echo "  git push origin v2025.34-IW.X"
else
    echo -e "${RED}❌ Some checks failed. Please fix issues before creating tag.${NC}"
    exit 1
fi