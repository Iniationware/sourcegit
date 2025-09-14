#!/bin/bash

# GitHub CLI Installation Check
if ! command -v gh &> /dev/null; then
    echo "GitHub CLI (gh) is not installed!"
    echo "Install it with: brew install gh"
    echo "Then authenticate with: gh auth login"
    exit 1
fi

# Check if authenticated
if ! gh auth status &> /dev/null; then
    echo "Not authenticated with GitHub!"
    echo "Run: gh auth login"
    exit 1
fi

TAG="v2025.34-IW.7"

echo "Creating GitHub Release for $TAG..."

# Create release with release notes
gh release create "$TAG" \
    --title "v2025.34-IW.7 - Repository Metrics Dashboard" \
    --notes "## Iniationware Custom Release

### ✨ New Features
- **Repository Metrics Dashboard**
  - Branch Counter showing local/remote branches (7/12 format)
  - Commit Statistics with Today/Week/Month tracking (T:4 W:48 M:82)
  - Two-row status panel for better space utilization
  - Activity level indicators with detailed tooltips

### 🐛 Bug Fixes
- Fixed all IDE0005 build errors across entire codebase
- Improved shutdown performance (30s → 0.5s)
- Enhanced memory management and cache cleanup
- Fixed GitFlow sidebar refresh issues

### 🔧 Technical Improvements
- Real Git commands (no simulations)
- Comprehensive error handling
- Auto-refresh on repository state changes
- 100% read-only operations for safety
- Added local build validation scripts

### 📝 Development Tools
- \`check_build.sh\` - Pre-commit validation
- \`test_all_ide0005.sh\` - Complete IDE0005 check

This release is based on SourceGit v2025.34 with Iniationware enhancements.

**Note**: Packages are being built by GitHub Actions and will be available shortly." \
    --prerelease

echo "✅ Release created!"
echo ""
echo "View it at: https://github.com/Iniationware/sourcegit/releases/tag/$TAG"
echo ""
echo "The GitHub Actions workflow should now build and attach the packages automatically."