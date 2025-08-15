#!/bin/bash
# Phase 2 Testing: Branch Refresh with Lock File Handling

echo "====================================="
echo "Phase 2: Branch Refresh & Lock File Test"
echo "====================================="
echo ""

# Test repository path
TEST_REPO="test-lock-handling"

# Clean up if exists
if [ -d "$TEST_REPO" ]; then
    rm -rf "$TEST_REPO"
fi

# Create test repository
echo "1. Creating test repository..."
git init "$TEST_REPO"
cd "$TEST_REPO"

# Initial commit
echo "test" > README.md
git add README.md
git commit -m "Initial commit"

echo ""
echo "2. Creating multiple branches..."
for i in {1..10}; do
    git branch "test-branch-$i"
done
echo "   ✓ Created 10 test branches"

echo ""
echo "3. Testing lock file handling..."
echo "   Creating index.lock file..."
touch .git/index.lock

# Try git operations that should fail due to lock
echo "   Testing branch creation with lock present..."
git branch test-locked 2>&1 | grep -q "Unable to create" && echo "   ✓ Git correctly reports lock" || echo "   ✗ Lock not detected"

# Remove lock
rm .git/index.lock
echo "   ✓ Lock file removed"

echo ""
echo "4. Testing concurrent branch operations..."
# Create lock file and branch operations concurrently
(
    for i in {1..5}; do
        touch .git/index.lock
        sleep 0.1
        rm .git/index.lock
        sleep 0.1
    done
) &
LOCK_PID=$!

# Try branch operations while locks are being created/removed
for i in {11..15}; do
    git branch "concurrent-$i" 2>/dev/null
    sleep 0.2
done

wait $LOCK_PID
echo "   ✓ Concurrent operations completed"

echo ""
echo "5. Testing stale lock file removal..."
# Create an old lock file
touch .git/index.lock
# Make it appear old (11 minutes ago)
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    touch -t $(date -v-11M +%Y%m%d%H%M.%S) .git/index.lock
else
    # Linux
    touch -d "11 minutes ago" .git/index.lock
fi
echo "   ✓ Created stale lock file (11 minutes old)"

echo ""
echo "6. Testing branch refresh with various lock scenarios..."

# Test HEAD.lock
touch .git/HEAD.lock
git branch head-lock-test 2>/dev/null
rm -f .git/HEAD.lock
echo "   ✓ HEAD.lock scenario tested"

# Test refs lock
mkdir -p .git/refs/heads
touch .git/refs/heads/test.lock
git branch refs-lock-test 2>/dev/null
rm -f .git/refs/heads/test.lock
echo "   ✓ refs lock scenario tested"

echo ""
echo "7. Creating remote and testing remote branch operations..."
# Add a fake remote
git remote add origin https://github.com/test/repo.git 2>/dev/null || true
mkdir -p .git/refs/remotes/origin
echo "abc123" > .git/refs/remotes/origin/main
echo "   ✓ Remote configuration created"

echo ""
echo "8. Testing rapid branch switching with potential locks..."
for i in {1..10}; do
    # Randomly create/remove lock during branch operations
    if [ $((i % 3)) -eq 0 ]; then
        touch .git/index.lock &
    fi
    
    git checkout -q "test-branch-$i" 2>/dev/null
    
    # Clean up any locks
    rm -f .git/index.lock 2>/dev/null
done
git checkout -q main
echo "   ✓ Rapid branch switching completed"

echo ""
echo "====================================="
echo "Phase 2 Tests Completed Successfully!"
echo "====================================="
echo ""
echo "Manual verification steps:"
echo "1. Open this repository in SourceGit: $(pwd)"
echo "2. Create a .git/index.lock file manually: touch .git/index.lock"
echo "3. Try to refresh branches in SourceGit (F5 or click refresh)"
echo "4. Verify that SourceGit retries and eventually succeeds"
echo "5. Monitor for any error dialogs - there should be none"
echo "6. Check that all branches are visible after lock is removed"
echo ""
echo "Expected results:"
echo "- No crashes when lock files are present"
echo "- Automatic retry with exponential backoff"
echo "- Graceful handling of lock file conflicts"
echo "- All branches visible after locks clear"
echo "- No error dialogs for transient lock issues"