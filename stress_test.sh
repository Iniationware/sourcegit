#!/bin/bash
# Stress test for SourceGit lock handling

echo "=== SourceGit Stress Test ==="
echo "Open SourceGit with this repository first!"
echo "Press Enter when ready..."
read

REPO_PATH=$(pwd)

echo "Starting stress test..."

# Test 1: Rapid lock creation/removal
echo "Test 1: Rapid lock file creation..."
for i in {1..20}; do
    touch .git/index.lock
    sleep 0.1
    rm -f .git/index.lock
    sleep 0.1
    echo -n "."
done
echo " Done!"

# Test 2: Concurrent branch operations
echo "Test 2: Concurrent branch creation..."
for i in {1..10}; do
    git branch "stress-test-$i" &
done
wait
echo "Created 10 branches concurrently"

# Test 3: Rapid branch switching
echo "Test 3: Rapid branch switching..."
for i in {1..10}; do
    git checkout -q "stress-test-$i" 2>/dev/null
    echo -n "."
done
git checkout -q master 2>/dev/null
echo " Done!"

# Test 4: Lock during operations
echo "Test 4: Creating lock during refresh..."
touch .git/index.lock
echo "Lock created - try refreshing in SourceGit now"
sleep 3
rm .git/index.lock
echo "Lock removed"

echo ""
echo "=== Stress Test Complete ==="
echo "Check SourceGit for:"
echo "- No crashes"
echo "- All branches visible"
echo "- Memory usage stable"
echo "- UI still responsive"