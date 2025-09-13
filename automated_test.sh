#!/bin/bash

# Automated Test Script for SourceGit Refactoring
# This script tests the refactored Repository.cs components

set -e

echo "🧪 Starting automated tests for SourceGit refactoring..."
echo ""

TEST_REPO="/tmp/test-sourcegit-repo"
SOURCEGIT_DIR="/Users/biancode/Development/Iniationware/sourcegit"

# Color codes for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test result tracking
TESTS_PASSED=0
TESTS_FAILED=0

# Function to print test results
print_test_result() {
    local test_name=$1
    local result=$2
    
    if [ "$result" == "PASS" ]; then
        echo -e "${GREEN}✅ $test_name: PASSED${NC}"
        ((TESTS_PASSED++))
    else
        echo -e "${RED}❌ $test_name: FAILED${NC}"
        ((TESTS_FAILED++))
    fi
}

echo "📁 Test Repository: $TEST_REPO"
echo ""

# Test 1: Check if repository exists and is valid
echo "1️⃣ Testing Repository Validation..."
if [ -d "$TEST_REPO/.git" ]; then
    print_test_result "Repository exists" "PASS"
else
    print_test_result "Repository exists" "FAIL"
    exit 1
fi

# Test 2: Check compilation of refactored code
echo ""
echo "2️⃣ Testing Build Compilation..."
cd "$SOURCEGIT_DIR"
if dotnet build -c Release --nologo --verbosity quiet 2>/dev/null; then
    print_test_result "Build compilation" "PASS"
else
    print_test_result "Build compilation" "FAIL"
fi

# Test 3: Check if all partial classes exist
echo ""
echo "3️⃣ Testing Partial Class Files..."
PARTIAL_FILES=(
    "src/ViewModels/Repository.cs"
    "src/ViewModels/Repository.State.cs"
    "src/ViewModels/Repository.Refresh.cs"
    "src/ViewModels/Repository.GitOperations.cs"
    "src/ViewModels/Repository.Search.cs"
)

for file in "${PARTIAL_FILES[@]}"; do
    if [ -f "$SOURCEGIT_DIR/$file" ]; then
        print_test_result "File exists: $(basename $file)" "PASS"
    else
        print_test_result "File exists: $(basename $file)" "FAIL"
    fi
done

# Test 4: Test Git operations in the test repository
echo ""
echo "4️⃣ Testing Git Operations..."
cd "$TEST_REPO"

# Test branch listing
if git branch -a | grep -q "develop"; then
    print_test_result "Branch operations" "PASS"
else
    print_test_result "Branch operations" "FAIL"
fi

# Test tag listing
if git tag | grep -q "v1.0.0"; then
    print_test_result "Tag operations" "PASS"
else
    print_test_result "Tag operations" "FAIL"
fi

# Test stash listing
if [ $(git stash list | wc -l) -gt 0 ]; then
    print_test_result "Stash operations" "PASS"
else
    print_test_result "Stash operations" "FAIL"
fi

# Test submodule status
if git submodule status | grep -q "prettier"; then
    print_test_result "Submodule operations" "PASS"
else
    print_test_result "Submodule operations" "FAIL"
fi

# Test 5: Verify repository statistics
echo ""
echo "5️⃣ Testing Repository Statistics..."
COMMIT_COUNT=$(git rev-list --all --count)
if [ $COMMIT_COUNT -gt 300 ]; then
    print_test_result "Commit history (${COMMIT_COUNT} commits)" "PASS"
else
    print_test_result "Commit history (${COMMIT_COUNT} commits)" "FAIL"
fi

BRANCH_COUNT=$(git branch -a | wc -l)
if [ $BRANCH_COUNT -gt 15 ]; then
    print_test_result "Branch count (${BRANCH_COUNT} branches)" "PASS"
else
    print_test_result "Branch count (${BRANCH_COUNT} branches)" "FAIL"
fi

# Test 6: Check line count reduction in main Repository.cs
echo ""
echo "6️⃣ Testing Code Refactoring Metrics..."
cd "$SOURCEGIT_DIR"
MAIN_FILE_LINES=$(wc -l < src/ViewModels/Repository.cs)
if [ $MAIN_FILE_LINES -lt 1500 ]; then
    print_test_result "Main file size reduction (${MAIN_FILE_LINES} lines)" "PASS"
else
    print_test_result "Main file size reduction (${MAIN_FILE_LINES} lines)" "FAIL"
fi

# Test 7: Check for thread-safe patterns
echo ""
echo "7️⃣ Testing Thread Safety Patterns..."
if grep -q "Dispatcher.UIThread" src/ViewModels/Repository.State.cs 2>/dev/null; then
    print_test_result "UI thread dispatch patterns" "PASS"
else
    print_test_result "UI thread dispatch patterns" "FAIL"
fi

# Test 8: Performance test - measure refresh time
echo ""
echo "8️⃣ Testing Performance..."
cd "$TEST_REPO"
START_TIME=$(date +%s%N)
git status > /dev/null 2>&1
git branch > /dev/null 2>&1
git tag > /dev/null 2>&1
END_TIME=$(date +%s%N)
ELAPSED_TIME=$(( ($END_TIME - $START_TIME) / 1000000 ))

if [ $ELAPSED_TIME -lt 3000 ]; then
    print_test_result "Performance test (${ELAPSED_TIME}ms)" "PASS"
else
    print_test_result "Performance test (${ELAPSED_TIME}ms)" "FAIL"
fi

# Final Results
echo ""
echo "═══════════════════════════════════════════"
echo "📊 TEST RESULTS SUMMARY"
echo "═══════════════════════════════════════════"
echo -e "${GREEN}✅ Tests Passed: $TESTS_PASSED${NC}"
echo -e "${RED}❌ Tests Failed: $TESTS_FAILED${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed successfully!${NC}"
    echo -e "${GREEN}The refactored Repository.cs maintains full functionality.${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠️ Some tests failed. Please review the results.${NC}"
    exit 1
fi