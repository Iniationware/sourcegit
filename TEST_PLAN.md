# Repository Refactoring Test Plan

## Test Objectives
Verify that the refactored Repository.cs (split into partial classes) maintains all functionality without regressions.

## Test Environment
- **Application**: SourceGit
- **Framework**: .NET 9.0 with Avalonia UI
- **Refactored Files**:
  - Repository.cs (main)
  - Repository.State.cs (shared state)
  - Repository.Refresh.cs (refresh operations)
  - Repository.GitOperations.cs (git operations)
  - Repository.Search.cs (search functionality)

## Test Categories

### 1. Core Functionality Tests

#### 1.1 Repository Loading
- [ ] Open existing repository
- [ ] Clone new repository
- [ ] Initialize new repository
- [ ] Open bare repository
- [ ] Handle invalid repository paths

#### 1.2 State Management
- [ ] Repository settings persistence
- [ ] Filter states (branch/tag filters)
- [ ] Search state preservation
- [ ] UI state synchronization

### 2. Refresh Operations Tests (Repository.Refresh.cs)

#### 2.1 Individual Refresh Methods
- [ ] RefreshBranches() - Local and remote branches
- [ ] RefreshTags() - All tags loaded correctly
- [ ] RefreshCommits() - Commit history with graph
- [ ] RefreshWorkingCopyChanges() - Staged/unstaged files
- [ ] RefreshStashes() - Stash list
- [ ] RefreshWorktrees() - Worktree information
- [ ] RefreshSubmodules() - Submodule status

#### 2.2 Composite Refresh Operations
- [ ] RefreshAll() - Full repository refresh
- [ ] RefreshAfterOperation() - Post-operation updates
- [ ] Auto-refresh on file system changes
- [ ] Parallel refresh performance

### 3. Git Operations Tests (Repository.GitOperations.cs)

#### 3.1 Branch Operations
- [ ] Create new branch
- [ ] Delete branch (local/remote)
- [ ] Rename branch
- [ ] Checkout branch
- [ ] Merge branches
- [ ] Rebase operations
- [ ] Cherry-pick commits

#### 3.2 Remote Operations
- [ ] Fetch from remote
- [ ] Pull changes
- [ ] Push changes
- [ ] Add/remove remotes
- [ ] Prune remote branches

#### 3.3 Commit Operations
- [ ] Stage files
- [ ] Unstage files
- [ ] Create commits
- [ ] Amend commits
- [ ] Revert commits
- [ ] Reset (soft/mixed/hard)

#### 3.4 Tag Operations
- [ ] Create annotated tag
- [ ] Create lightweight tag
- [ ] Delete tags
- [ ] Push tags to remote

#### 3.5 Stash Operations
- [ ] Create stash
- [ ] Apply stash
- [ ] Pop stash
- [ ] Drop stash
- [ ] Clear all stashes

### 4. Search Operations Tests (Repository.Search.cs)

#### 4.1 Commit Search
- [ ] Search by commit message
- [ ] Search by author
- [ ] Search by file path
- [ ] Search with filters active
- [ ] Clear search results

#### 4.2 File Search
- [ ] Search in working directory
- [ ] Search in specific revision
- [ ] Navigate search results
- [ ] Performance with large repositories

### 5. UI Thread Safety Tests

#### 5.1 Dispatcher Operations
- [ ] UI updates from background threads
- [ ] Concurrent refresh operations
- [ ] Progress indicator updates
- [ ] Error message display

### 6. Performance Tests

#### 6.1 Large Repository Handling
- [ ] Repository with 10,000+ commits
- [ ] Repository with 100+ branches
- [ ] Repository with 1,000+ files
- [ ] Memory usage monitoring
- [ ] Response time measurements

#### 6.2 Concurrent Operations
- [ ] Multiple refresh operations
- [ ] Simultaneous git operations
- [ ] File system watcher events
- [ ] UI responsiveness

### 7. Integration Tests

#### 7.1 Cross-Component Integration
- [ ] Repository → Histories view
- [ ] Repository → WorkingCopy view
- [ ] Repository → Statistics view
- [ ] Repository → Branch tree
- [ ] Repository → Submodules

#### 7.2 External Tool Integration
- [ ] Open in terminal
- [ ] Open in file explorer
- [ ] Open in external editor
- [ ] Git flow operations

### 8. Error Handling Tests

#### 8.1 Git Command Failures
- [ ] Network disconnection during fetch/pull/push
- [ ] Merge conflicts
- [ ] Permission denied errors
- [ ] Invalid git commands

#### 8.2 File System Issues
- [ ] Repository deleted while open
- [ ] Permission changes
- [ ] Disk space issues
- [ ] File locks

## Test Data Requirements

### Demo Repository Structure
- **Branches**: main, develop, feature/*, release/*, hotfix/*
- **Tags**: v1.0.0, v1.1.0, v2.0.0-beta
- **Commits**: 500+ commits with various patterns
- **Files**: Source code, documentation, binary files
- **Submodules**: At least 2 submodules
- **Stashes**: 5+ stashed changes
- **Remotes**: origin, upstream
- **Merge History**: Including merge commits and conflicts
- **Large Files**: Test performance with larger files

## Test Execution Steps

1. **Setup Phase**
   - Create complex demo repository
   - Generate test data
   - Configure test environment

2. **Functional Testing**
   - Execute each test category
   - Document results
   - Capture any errors or issues

3. **Performance Testing**
   - Measure operation times
   - Monitor resource usage
   - Test with large datasets

4. **Regression Testing**
   - Compare with original behavior
   - Verify no functionality lost
   - Check for new issues

## Success Criteria

- ✅ All functional tests pass
- ✅ No regression from original functionality
- ✅ Performance within acceptable limits (<3s for refresh operations)
- ✅ Memory usage stable (no leaks)
- ✅ UI remains responsive during operations
- ✅ Thread safety maintained
- ✅ Error handling works correctly

## Risk Areas

### High Priority
- Thread safety in UI updates
- Parallel refresh operations
- State synchronization across partial classes
- Git operation error handling

### Medium Priority
- Performance with large repositories
- Memory management
- Search functionality
- Filter combinations

### Low Priority
- Edge cases in git operations
- Rare error conditions
- Platform-specific issues

## Test Results Documentation

### Template for Recording Results
```markdown
Test: [Test Name]
Date: [Date]
Status: [Pass/Fail/Blocked]
Notes: [Any observations]
Issues Found: [Issue description if any]
```

## Post-Testing Actions

1. Document all issues found
2. Prioritize fixes based on severity
3. Re-test after fixes
4. Update documentation
5. Performance optimization if needed