#!/bin/bash
echo "=== Verifying Phase 2 Fix ==="
echo ""

# Check if lock exists
if [ -f ".git/index.lock" ]; then
    echo "✅ Lock file exists (this blocks Git operations)"
    echo "   Git commands will fail with 'File exists' error"
    echo ""
    echo "Now in SourceGit:"
    echo "1. Try to refresh (F5) - should NOT crash"
    echo "2. Try to create a branch - should retry gracefully"
    echo ""
    echo "To remove the lock and let operations succeed:"
    echo "   rm .git/index.lock"
else
    echo "❌ No lock file found"
    echo "   Create one with: touch .git/index.lock"
fi

echo ""
echo "Testing Git behavior with lock:"
git branch test-with-lock 2>&1 | head -3
echo ""
echo "This error is EXPECTED when lock exists."
echo "SourceGit should handle this gracefully without crashing."
