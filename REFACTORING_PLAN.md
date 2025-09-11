# Repository.cs Refactoring Plan

## Current State
- Repository.cs has 2549 lines of code
- Successfully converted to partial class
- Build is working

## Refactoring Strategy

### Phase 1: Preparation ✅ COMPLETED
- [x] Create IRepositoryService interface
- [x] Convert Repository.cs to partial class declaration
- [x] Ensure build still works

### Phase 2: Gradual Extraction (RECOMMENDED APPROACH)

#### Step 1: Extract Refresh Operations
Create `Repository.Refresh.cs` with:
- RefreshAfterOperation()
- RefreshAll()
- RefreshBranches()
- RefreshTags()
- RefreshCommits()
- RefreshWorkingCopyChanges()
- RefreshStashes()
- RefreshSubmodules()
- RefreshWorktrees()

**Note**: These methods have complex dependencies on internal fields and other methods. Need careful extraction.

#### Step 2: Extract Git Operations
Create `Repository.GitOperations.cs` with:
- FetchAsync()
- PullAsync()
- PushAsync()
- CheckoutBranchAsync()
- CreateBranch()
- DeleteBranch()
- MergeMultipleBranches()

#### Step 3: Extract Branch Management
Create `Repository.Branches.cs` with:
- Branch tree building methods
- Branch filtering methods
- GitFlow related methods
- Branch context menu creation

#### Step 4: Extract UI/Popup Management
Create `Repository.UI.cs` with:
- ShowPopup()
- ShowAndStartPopupAsync()
- CanCreatePopup()
- All popup creation methods

#### Step 5: Extract Search Operations
Create `Repository.Search.cs` with:
- StartSearchCommits()
- CalcWorktreeFilesForSearching()
- CalcMatchedFilesForSearching()
- Search-related properties

## Key Challenges Identified

1. **Complex Dependencies**: Methods are tightly coupled with private fields and other methods
2. **UI Thread Dispatching**: Many methods use Dispatcher.UIThread.Invoke
3. **Async/Await Patterns**: Mix of synchronous and asynchronous operations
4. **State Management**: Extensive use of private fields that need to be accessible across partial classes

## Recommendations

1. **Start Small**: Begin with methods that have fewer dependencies
2. **Test Incrementally**: Build and test after each extraction
3. **Keep Related Methods Together**: Group methods that work closely together
4. **Document Dependencies**: Note which fields and methods each extracted group needs
5. **Consider Service Pattern**: Eventually move to service-based architecture for better separation

## Alternative Approach: Service-Based Architecture

Instead of just splitting into partial classes, consider:

1. **Create Service Interfaces**:
   - IGitOperationService
   - IRefreshService
   - IBranchManagementService
   - ISearchService

2. **Implement Services**: Each service handles its specific domain

3. **Use Dependency Injection**: Repository becomes a coordinator that delegates to services

4. **Benefits**:
   - Better testability
   - Clear separation of concerns
   - Easier to maintain and extend
   - Follows SOLID principles

## Next Steps

1. Attempt careful extraction of refresh methods with proper dependency handling
2. If extraction proves too complex, maintain current structure but document sections
3. Consider gradual migration to service pattern over time
4. Add unit tests for extracted components