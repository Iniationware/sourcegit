#!/bin/bash
# Phase 1 Testing: File Watcher Thread Safety

echo "===================================="
echo "Phase 1: File Watcher Thread Safety Test"
echo "===================================="
echo ""

# Test repository path
TEST_REPO="test-thread-safety"

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
echo "2. Testing rapid file creation/deletion (100 files)..."
echo "   This tests thread-safe event handling"

# Rapid file operations to test thread safety
for i in {1..100}; do
    touch "test$i.txt" &
done
wait

for i in {1..100}; do
    rm "test$i.txt" &
done
wait

echo "   ✓ Rapid file operations completed"

echo ""
echo "3. Testing concurrent Git operations..."
echo "   This tests lock handling and retry logic"

# Create multiple branches concurrently
git branch test1 &
git branch test2 &
git branch test3 &
wait

echo "   ✓ Concurrent branch creation completed"

echo ""
echo "4. Testing Git lock file handling..."
# Create a lock file
touch .git/index.lock
echo "   Created .git/index.lock"
sleep 1
rm .git/index.lock
echo "   Removed .git/index.lock"
echo "   ✓ Lock file test completed"

echo ""
echo "5. Testing submodule path changes..."
# Test submodule-like paths
mkdir -p submodule1/nested
echo "test" > submodule1/nested/file.txt
echo "   ✓ Submodule path test completed"

echo ""
echo "6. Creating many branches for stress test..."
for i in {1..50}; do
    git branch "stress-test-$i"
done
echo "   ✓ Created 50 branches"

echo ""
echo "7. Testing rapid branch switching..."
for i in {1..10}; do
    git checkout -q "stress-test-$i"
done
git checkout -q main
echo "   ✓ Rapid branch switching completed"

echo ""
echo "===================================="
echo "Phase 1 Tests Completed Successfully!"
echo "===================================="
echo ""
echo "Manual verification steps:"
echo "1. Open this repository in SourceGit: $(pwd)"
echo "2. Monitor memory usage in Activity Monitor"
echo "3. Try creating/deleting files while SourceGit is open"
echo "4. Verify no crashes occur"
echo "5. Check that branch list updates correctly"
echo ""
echo "Expected results:"
echo "- No crashes during file operations"
echo "- Memory usage stable"
echo "- UI remains responsive"
echo "- All branches visible in SourceGit"