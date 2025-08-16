using System.Collections.Generic;
using System.Linq;

namespace SourceGit.Models
{
    public enum GitFlowBranchType
    {
        None = 0,
        Master,
        Develop,
        Feature,
        Release,
        Hotfix,
        Support
    }

    public class GitFlow
    {
        public string Master { get; set; } = "master";
        public string Develop { get; set; } = "develop";
        public string FeaturePrefix { get; set; } = "feature/";
        public string ReleasePrefix { get; set; } = "release/";
        public string HotfixPrefix { get; set; } = "hotfix/";
        public string SupportPrefix { get; set; } = "support/";
        public string VersionTagPrefix { get; set; } = "";

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(Master) &&
                    !string.IsNullOrEmpty(Develop) &&
                    !string.IsNullOrEmpty(FeaturePrefix) &&
                    !string.IsNullOrEmpty(ReleasePrefix) &&
                    !string.IsNullOrEmpty(HotfixPrefix);
            }
        }

        public string GetPrefix(GitFlowBranchType type)
        {
            return type switch
            {
                GitFlowBranchType.Feature => FeaturePrefix,
                GitFlowBranchType.Release => ReleasePrefix,
                GitFlowBranchType.Hotfix => HotfixPrefix,
                GitFlowBranchType.Support => SupportPrefix,
                _ => string.Empty,
            };
        }

        public GitFlowBranchType GetBranchType(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
                return GitFlowBranchType.None;

            // Remove refs/heads/ prefix if present
            if (branchName.StartsWith("refs/heads/"))
                branchName = branchName.Substring(11);

            // Check main branches
            if (branchName == Master)
                return GitFlowBranchType.Master;
            if (branchName == Develop)
                return GitFlowBranchType.Develop;

            // Check prefixed branches
            if (branchName.StartsWith(FeaturePrefix))
                return GitFlowBranchType.Feature;
            if (branchName.StartsWith(ReleasePrefix))
                return GitFlowBranchType.Release;
            if (branchName.StartsWith(HotfixPrefix))
                return GitFlowBranchType.Hotfix;
            if (!string.IsNullOrEmpty(SupportPrefix) && branchName.StartsWith(SupportPrefix))
                return GitFlowBranchType.Support;

            return GitFlowBranchType.None;
        }

        public string GetDisplayName(string branchName, GitFlowBranchType type)
        {
            if (string.IsNullOrEmpty(branchName))
                return branchName;

            // Remove refs/heads/ prefix if present
            if (branchName.StartsWith("refs/heads/"))
                branchName = branchName.Substring(11);

            return type switch
            {
                GitFlowBranchType.Feature => branchName.Substring(FeaturePrefix.Length),
                GitFlowBranchType.Release => branchName.Substring(ReleasePrefix.Length),
                GitFlowBranchType.Hotfix => branchName.Substring(HotfixPrefix.Length),
                GitFlowBranchType.Support => !string.IsNullOrEmpty(SupportPrefix) ? branchName.Substring(SupportPrefix.Length) : branchName,
                _ => branchName
            };
        }

        public List<GitFlowBranchGroup> GroupBranches(List<Branch> branches)
        {
            var groups = new List<GitFlowBranchGroup>();

            // Create groups for each type
            var featureGroup = new GitFlowBranchGroup { Type = GitFlowBranchType.Feature, Name = "Features" };
            var releaseGroup = new GitFlowBranchGroup { Type = GitFlowBranchType.Release, Name = "Releases" };
            var hotfixGroup = new GitFlowBranchGroup { Type = GitFlowBranchType.Hotfix, Name = "Hotfixes" };
            var supportGroup = new GitFlowBranchGroup { Type = GitFlowBranchType.Support, Name = "Support" };

            foreach (var branch in branches)
            {
                var type = GetBranchType(branch.Name);
                switch (type)
                {
                    case GitFlowBranchType.Feature:
                        featureGroup.Branches.Add(branch);
                        break;
                    case GitFlowBranchType.Release:
                        releaseGroup.Branches.Add(branch);
                        break;
                    case GitFlowBranchType.Hotfix:
                        hotfixGroup.Branches.Add(branch);
                        break;
                    case GitFlowBranchType.Support:
                        supportGroup.Branches.Add(branch);
                        break;
                }
            }

            // Only add groups that have branches
            if (featureGroup.Branches.Any())
                groups.Add(featureGroup);
            if (releaseGroup.Branches.Any())
                groups.Add(releaseGroup);
            if (hotfixGroup.Branches.Any())
                groups.Add(hotfixGroup);
            if (supportGroup.Branches.Any())
                groups.Add(supportGroup);

            return groups;
        }
    }

    public class GitFlowBranchGroup
    {
        public GitFlowBranchType Type { get; set; }
        public string Name { get; set; }
        public List<Branch> Branches { get; set; } = new List<Branch>();
        public bool IsExpanded { get; set; } = true;
    }
}
