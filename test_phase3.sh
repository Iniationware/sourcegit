#!/bin/bash
# Phase 3 Testing: Memory Leak Prevention

echo "====================================="
echo "Phase 3: Memory Leak Prevention Test"
echo "====================================="
echo ""

# Test repository path
TEST_REPO="test-memory-leaks"

# Clean up if exists
if [ -d "$TEST_REPO" ]; then
    rm -rf "$TEST_REPO"
fi

# Create test repository with lots of commits
echo "1. Creating test repository with many commits..."
git init "$TEST_REPO"
cd "$TEST_REPO"

# Create many commits to stress test user cache
echo "2. Creating 1000 commits with different authors..."
for i in {1..1000}; do
    echo "commit $i" > "file$((i % 10)).txt"
    git add -A
    GIT_AUTHOR_NAME="Author$i" GIT_AUTHOR_EMAIL="author$i@test.com" \
    GIT_COMMITTER_NAME="Author$i" GIT_COMMITTER_EMAIL="author$i@test.com" \
    git commit -m "Commit $i" --quiet
    
    if [ $((i % 100)) -eq 0 ]; then
        echo "   Created $i commits..."
    fi
done
echo "   ✓ Created 1000 commits"

echo ""
echo "3. Creating many branches..."
for i in {1..100}; do
    git branch "feature-$i" --quiet
done
echo "   ✓ Created 100 branches"

echo ""
echo "4. Creating tags..."
for i in {1..50}; do
    git tag "v1.$i.0" --quiet
done
echo "   ✓ Created 50 tags"

echo ""
echo "====================================="
echo "Memory Test Instructions"
echo "====================================="
echo ""
echo "1. Open Activity Monitor (macOS) or Task Manager (Windows)"
echo "2. Note the memory usage of SourceGit"
echo "3. Open this repository in SourceGit: $(pwd)"
echo "4. Navigate through the history (scroll up/down)"
echo "5. Switch between branches multiple times"
echo "6. Open and close the repository several times"
echo "7. Monitor memory usage - it should:"
echo "   - Not continuously grow"
echo "   - Return to baseline after closing repository"
echo "   - Stay under 500MB for normal usage"
echo ""
echo "Expected results:"
echo "- Memory usage stabilizes after initial loading"
echo "- Closing repository releases memory"
echo "- User cache doesn't grow beyond 5000 entries"
echo "- Performance data is trimmed periodically"
echo ""
echo "To monitor in code:"
echo "- User cache size: Models.User.GetCacheSize()"
echo "- Performance data: Models.PerformanceMonitor.GetMeasurementCount()"
echo ""
echo "Memory leak indicators to watch for:"
echo "❌ Memory continuously increasing"
echo "❌ Memory not released after closing"
echo "❌ Excessive memory usage (>1GB)"
echo "✅ Memory stabilizes after operations"
echo "✅ Memory returns to baseline"
echo "✅ Reasonable memory usage (<500MB)"