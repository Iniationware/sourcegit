using System;

namespace SourceGit.Commands
{
    public class GitFlowStart : Command
    {
        public GitFlowStart(string repo, Models.GitFlowBranchType type, string name, string baseBranch = null)
        {
            WorkingDirectory = repo;
            Context = repo;
            
            var config = new Models.GitFlow();
            // Load config from repository settings if needed
            
            switch (type)
            {
                case Models.GitFlowBranchType.Feature:
                    Args = $"checkout -b {config.FeaturePrefix}{name}";
                    if (!string.IsNullOrEmpty(baseBranch))
                        Args += $" {baseBranch}";
                    else
                        Args += $" {config.Develop}";
                    break;
                    
                case Models.GitFlowBranchType.Release:
                    Args = $"checkout -b {config.ReleasePrefix}{name}";
                    if (!string.IsNullOrEmpty(baseBranch))
                        Args += $" {baseBranch}";
                    else
                        Args += $" {config.Develop}";
                    break;
                    
                case Models.GitFlowBranchType.Hotfix:
                    Args = $"checkout -b {config.HotfixPrefix}{name}";
                    if (!string.IsNullOrEmpty(baseBranch))
                        Args += $" {baseBranch}";
                    else
                        Args += $" {config.Master}";
                    break;
                    
                case Models.GitFlowBranchType.Support:
                    if (string.IsNullOrEmpty(baseBranch))
                        throw new ArgumentException("Support branches require a base branch");
                    Args = $"checkout -b {config.SupportPrefix}{name} {baseBranch}";
                    break;
                    
                default:
                    throw new ArgumentException($"Cannot start branch of type {type}");
            }
        }
    }
}