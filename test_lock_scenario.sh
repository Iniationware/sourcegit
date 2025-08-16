#!/bin/bash
echo "=== Quick Lock File Test for SourceGit ==="
echo ""

# Create a test repo
TEST_DIR="test-sourcegit-locks"
rm -rf $TEST_DIR
git init $TEST_DIR
cd $TEST_DIR

# Create initial content
echo "test" > README.md
git add README.md
git commit -m "Initial commit"

# Create some branches
for i in {1..5}; do
    git branch "feature-$i"
done

echo "Test repository created at: $(pwd)"
echo ""
echo "TEST 1: Lock file simulation"
echo "------------------------------"
echo "1. Open SourceGit and add this repository: $(pwd)"
echo "2. In another terminal, run: touch $(pwd)/.git/index.lock"
echo "3. Try to refresh branches in SourceGit (press F5)"
echo "4. Remove lock: rm $(pwd)/.git/index.lock"
echo "5. Refresh again - it should work now"
echo ""
echo "TEST 2: Concurrent operations"
echo "------------------------------"
echo "While SourceGit is open, run this command:"
echo "  for i in {1..10}; do git branch test-\$i & done"
echo ""
echo "Expected: SourceGit should handle this gracefully without crashing"
