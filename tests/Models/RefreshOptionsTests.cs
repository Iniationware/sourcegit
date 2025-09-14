using FluentAssertions;
using SourceGit.Models;
using Xunit;

namespace SourceGit.Tests.Models
{
    public class RefreshOptionsTests
    {
        [Fact]
        public void ForPush_ShouldSetCorrectOptions_WhenPushingCurrentBranchWithTags()
        {
            // Arrange & Act
            var options = RefreshOptions.ForPush(pushedTags: true, isCurrentBranch: true);

            // Assert
            options.RefreshBranches.Should().BeTrue();
            options.RefreshTags.Should().BeTrue();
            options.RefreshCommits.Should().BeTrue();
            options.RefreshWorkingCopy.Should().BeFalse();
            options.RefreshStashes.Should().BeFalse();
            options.RefreshSubmodules.Should().BeFalse();
        }

        [Fact]
        public void ForPush_ShouldNotRefreshCommits_WhenNotCurrentBranch()
        {
            // Arrange & Act
            var options = RefreshOptions.ForPush(pushedTags: false, isCurrentBranch: false);

            // Assert
            options.RefreshBranches.Should().BeTrue();
            options.RefreshTags.Should().BeFalse();
            options.RefreshCommits.Should().BeFalse();
            options.RefreshWorkingCopy.Should().BeFalse();
        }

        [Fact]
        public void ForFetch_ShouldSetCorrectOptions()
        {
            // Arrange & Act
            var options = RefreshOptions.ForFetch(fetchedTags: true);

            // Assert
            options.RefreshBranches.Should().BeTrue();
            options.RefreshTags.Should().BeTrue();
            options.RefreshCommits.Should().BeTrue();
            options.RefreshWorkingCopy.Should().BeFalse();
            options.RefreshStashes.Should().BeFalse();
        }

        [Fact]
        public void ForFetch_ShouldNotRefreshTags_WhenTagsExcluded()
        {
            // Arrange & Act
            var options = RefreshOptions.ForFetch(fetchedTags: false);

            // Assert
            options.RefreshTags.Should().BeFalse();
            options.RefreshBranches.Should().BeTrue();
            options.RefreshCommits.Should().BeTrue();
        }

        [Fact]
        public void ForPull_ShouldRefreshEverythingNeeded()
        {
            // Arrange & Act
            var options = RefreshOptions.ForPull();

            // Assert
            options.RefreshBranches.Should().BeTrue();
            options.RefreshTags.Should().BeTrue();
            options.RefreshCommits.Should().BeTrue();
            options.RefreshWorkingCopy.Should().BeTrue();
            options.RefreshStashes.Should().BeFalse();
            options.RefreshSubmodules.Should().BeFalse();
        }

        [Fact]
        public void ForCommit_ShouldRefreshWorkingCopyAndCommits()
        {
            // Arrange & Act
            var options = RefreshOptions.ForCommit();

            // Assert
            options.RefreshBranches.Should().BeTrue();
            options.RefreshCommits.Should().BeTrue();
            options.RefreshWorkingCopy.Should().BeTrue();
            options.RefreshTags.Should().BeFalse();
            options.RefreshStashes.Should().BeFalse();
        }

        [Fact]
        public void ForStash_ShouldRefreshWorkingCopyAndStashes()
        {
            // Arrange & Act
            var options = RefreshOptions.ForStash();

            // Assert
            options.RefreshWorkingCopy.Should().BeTrue();
            options.RefreshStashes.Should().BeTrue();
            options.RefreshCommits.Should().BeFalse();
            options.RefreshBranches.Should().BeFalse();
            options.RefreshTags.Should().BeFalse();
        }

        [Fact]
        public void All_ShouldEnableAllRefreshOptions()
        {
            // Arrange & Act
            var options = RefreshOptions.All();

            // Assert
            options.RefreshBranches.Should().BeTrue();
            options.RefreshTags.Should().BeTrue();
            options.RefreshCommits.Should().BeTrue();
            options.RefreshWorkingCopy.Should().BeTrue();
            options.RefreshStashes.Should().BeTrue();
            options.RefreshSubmodules.Should().BeTrue();
        }
    }
}