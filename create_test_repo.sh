#!/bin/bash

# Create Complex Test Repository for SourceGit Testing
# This script generates a comprehensive git repository with various features

set -e

REPO_NAME="test-sourcegit-repo"
REPO_PATH="/tmp/$REPO_NAME"

echo "🚀 Creating complex test repository at $REPO_PATH"

# Clean up if exists
if [ -d "$REPO_PATH" ]; then
    echo "Removing existing test repository..."
    rm -rf "$REPO_PATH"
fi

# Create and initialize repository
mkdir -p "$REPO_PATH"
cd "$REPO_PATH"
git init
git config user.name "Test User"
git config user.email "test@example.com"

# Create initial structure
echo "📁 Creating initial project structure..."
mkdir -p src/{components,services,utils}
mkdir -p docs
mkdir -p tests/{unit,integration}
mkdir -p assets/{images,styles}

# Create README
cat > README.md << 'EOF'
# Test Repository for SourceGit

This is a complex test repository designed to test all features of SourceGit.

## Features
- Multiple branches with Git Flow
- Tagged releases
- Submodules
- Stashed changes
- Merge conflicts history
- Large commit history
EOF

# Create .gitignore
cat > .gitignore << 'EOF'
*.log
*.tmp
.DS_Store
node_modules/
build/
dist/
*.pyc
__pycache__/
.env
EOF

# Initial commit
git add .
git commit -m "Initial commit: Project setup"

# Create main branch structure
git checkout -b develop

# Function to create random file content
create_file() {
    local file=$1
    local content=$2
    echo "$content" > "$file"
    echo "// Generated on $(date)" >> "$file"
    echo "// Random content: $(openssl rand -hex 20)" >> "$file"
}

# Generate commits on develop branch
echo "📝 Generating commit history..."
for i in {1..50}; do
    create_file "src/components/Component$i.js" "export const Component$i = () => { return 'Component $i'; };"
    git add "src/components/Component$i.js"
    git commit -m "feat: Add Component$i"
done

# Create feature branches
echo "🌿 Creating feature branches..."
for feature in "user-auth" "payment-integration" "dashboard-ui" "api-refactor" "performance-opt"; do
    git checkout -b "feature/$feature" develop
    
    for j in {1..10}; do
        create_file "src/services/${feature}_$j.js" "// $feature implementation part $j"
        git add "src/services/${feature}_$j.js"
        git commit -m "feat($feature): Implement ${feature} part $j"
    done
    
    git checkout develop
    git merge --no-ff "feature/$feature" -m "Merge feature/$feature into develop"
done

# Create release branches and tags
echo "🏷️ Creating releases and tags..."
git checkout -b release/1.0.0 develop
echo "1.0.0" > VERSION
git add VERSION
git commit -m "chore: Bump version to 1.0.0"
git checkout master
git merge --no-ff release/1.0.0 -m "Release version 1.0.0"
git tag -a v1.0.0 -m "Version 1.0.0 - Initial release"
git checkout develop
git merge --no-ff release/1.0.0 -m "Merge release/1.0.0 back to develop"

# Create more development commits
for i in {51..100}; do
    create_file "src/utils/util$i.js" "export function util$i() { return $i; }"
    git add "src/utils/util$i.js"
    git commit -m "feat: Add utility function util$i"
done

# Create release 1.1.0
git checkout -b release/1.1.0 develop
echo "1.1.0" > VERSION
git add VERSION
git commit -m "chore: Bump version to 1.1.0"
git checkout master
git merge --no-ff release/1.1.0 -m "Release version 1.1.0"
git tag -a v1.1.0 -m "Version 1.1.0 - Feature release"
git checkout develop
git merge --no-ff release/1.1.0 -m "Merge release/1.1.0 back to develop"

# Create hotfix
echo "🔥 Creating hotfix branch..."
git checkout -b hotfix/security-fix master
create_file "src/security-patch.js" "// Critical security fix"
git add "src/security-patch.js"
git commit -m "fix: Apply critical security patch"
git checkout master
git merge --no-ff hotfix/security-fix -m "Merge hotfix/security-fix"
git tag -a v1.1.1 -m "Version 1.1.1 - Security hotfix"
git checkout develop
git merge --no-ff hotfix/security-fix -m "Merge hotfix/security-fix to develop"

# Create more feature branches (keep some unmerged)
echo "🌱 Creating unmerged feature branches..."
for feature in "experimental-feature" "new-ui" "refactoring"; do
    git checkout -b "feature/$feature" develop
    mkdir -p src/experimental
    for k in {1..5}; do
        create_file "src/experimental/${feature}_$k.js" "// Experimental: $feature - part $k"
        git add "src/experimental/${feature}_$k.js"
        git commit -m "wip($feature): Work in progress $k"
    done
done

# Create bugfix branches
echo "🐛 Creating bugfix branches..."
git checkout develop
for bug in "fix-memory-leak" "fix-ui-glitch" "fix-api-error"; do
    git checkout -b "bugfix/$bug" develop
    mkdir -p src/fixes
    create_file "src/fixes/$bug.js" "// Fix for $bug"
    git add "src/fixes/$bug.js"
    git commit -m "fix: Resolve $bug"
done

# Create stashes
echo "📦 Creating stashed changes..."
git checkout develop
for i in {1..5}; do
    echo "Stash content $i" > "stash_file_$i.txt"
    git add "stash_file_$i.txt"
    git stash push -m "WIP: Stash number $i"
done

# Add submodules
echo "📦 Adding submodules..."
git checkout develop
git submodule add https://github.com/prettier/prettier.git lib/prettier
git submodule add https://github.com/eslint/eslint.git lib/eslint
git commit -m "feat: Add prettier and eslint as submodules"

# Create some large files
echo "📄 Creating large files for performance testing..."
dd if=/dev/urandom of=assets/large-binary.dat bs=1M count=5 2>/dev/null
git add assets/large-binary.dat
git commit -m "test: Add large binary file for testing"

# Generate more commits for history
echo "🔄 Generating additional commit history..."
for i in {101..300}; do
    file_type=$((i % 3))
    case $file_type in
        0) 
            create_file "tests/unit/test$i.js" "describe('Test $i', () => { it('should pass', () => {}); });"
            git add "tests/unit/test$i.js"
            git commit -m "test: Add unit test $i"
            ;;
        1)
            create_file "docs/doc$i.md" "# Documentation $i\n\nContent for doc $i"
            git add "docs/doc$i.md"
            git commit -m "docs: Add documentation $i"
            ;;
        2)
            create_file "src/components/Advanced$i.jsx" "export const Advanced$i = () => <div>Advanced $i</div>;"
            git add "src/components/Advanced$i.jsx"
            git commit -m "feat: Add Advanced component $i"
            ;;
    esac
done

# Create a merge conflict scenario (on a separate branch)
echo "💥 Creating merge conflict scenario..."
git checkout -b conflict-branch-1 develop
echo "Version A of the file" > conflict-file.txt
git add conflict-file.txt
git commit -m "feat: Add conflict file version A"

git checkout develop
git checkout -b conflict-branch-2
echo "Version B of the file" > conflict-file.txt
git add conflict-file.txt
git commit -m "feat: Add conflict file version B"

# Create remote tracking branches simulation
echo "🌐 Setting up remote branches..."
git checkout master
git branch -f origin/master
git branch -f origin/develop develop
git branch -f origin/feature/user-auth feature/user-auth

# Create annotated tags
echo "🏷️ Creating additional tags..."
git tag -a v2.0.0-alpha -m "Version 2.0.0 Alpha Release" develop
git tag -a v2.0.0-beta -m "Version 2.0.0 Beta Release" develop
git tag -a v2.0.0-rc1 -m "Version 2.0.0 Release Candidate 1" develop

# Create signed commits (simulated)
git checkout develop
echo "Important security update" > SECURITY.md
git add SECURITY.md
git commit -m "security: Add security policy

This is a signed commit (simulated)
Signed-off-by: Test User <test@example.com>"

# Create commits with different authors
git -c user.name="Alice Developer" -c user.email="alice@example.com" \
    commit --allow-empty -m "feat: Alice's contribution"
git -c user.name="Bob Reviewer" -c user.email="bob@example.com" \
    commit --allow-empty -m "review: Bob's code review"

# Final statistics
echo ""
echo "✅ Test repository created successfully!"
echo "📊 Repository Statistics:"
echo "   - Total commits: $(git rev-list --all --count)"
echo "   - Branches: $(git branch -a | wc -l)"
echo "   - Tags: $(git tag | wc -l)"
echo "   - Stashes: $(git stash list | wc -l)"
echo "   - Submodules: $(git submodule status | wc -l)"
echo "   - Files: $(git ls-files | wc -l)"
echo ""
echo "📍 Repository location: $REPO_PATH"
echo ""
echo "🎯 You can now open this repository in SourceGit for testing!"
echo "   Use: $REPO_PATH"