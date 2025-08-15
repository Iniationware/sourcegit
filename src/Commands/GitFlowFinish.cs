using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class GitFlowFinish
    {
        private readonly string _repo;
        private readonly Models.GitFlow _config;
        private readonly Models.GitFlowBranchType _type;
        private readonly string _branch;
        private readonly bool _keepBranch;
        private readonly bool _squash;

        public GitFlowFinish(string repo, string branch, bool keepBranch = false, bool squash = false)
        {
            _repo = repo;
            _branch = branch;
            _keepBranch = keepBranch;
            _squash = squash;
            _config = new Models.GitFlow();
            _type = _config.GetBranchType(branch);
        }

        public async Task<bool> ExecuteAsync()
        {
            switch (_type)
            {
                case Models.GitFlowBranchType.Feature:
                    return await FinishFeatureAsync();
                case Models.GitFlowBranchType.Release:
                    return await FinishReleaseAsync();
                case Models.GitFlowBranchType.Hotfix:
                    return await FinishHotfixAsync();
                default:
                    throw new ArgumentException($"Cannot finish branch of type {_type}");
            }
        }

        private async Task<bool> FinishFeatureAsync()
        {
            // Checkout develop
            var checkout = new Checkout(_repo);
            if (!await checkout.BranchAsync(_config.Develop, false))
                return false;

            // Merge feature
            var mode = _squash ? "--squash" : "";
            var merge = new Merge(_repo, _branch, mode, false);
            if (!await merge.ExecAsync())
                return false;

            // Delete feature branch if not keeping
            if (!_keepBranch)
            {
                var delete = new Branch(_repo, _branch);
                await delete.DeleteLocalAsync();
            }

            return true;
        }

        private async Task<bool> FinishReleaseAsync()
        {
            var releaseName = GetBranchName();

            // Checkout master
            var checkout = new Checkout(_repo);
            if (!await checkout.BranchAsync(_config.Master, false))
                return false;

            // Merge release to master
            var mergeMaster = new Merge(_repo, _branch, "", false);
            if (!await mergeMaster.ExecAsync())
                return false;

            // Tag the release
            var tag = new Tag(_repo, releaseName);
            await tag.AddAsync(_config.Master, $"Release {releaseName}", false);

            // Checkout develop
            if (!await checkout.BranchAsync(_config.Develop, false))
                return false;

            // Merge release to develop
            var mergeDevelop = new Merge(_repo, _branch, "", false);
            if (!await mergeDevelop.ExecAsync())
                return false;

            // Delete release branch if not keeping
            if (!_keepBranch)
            {
                var delete = new Branch(_repo, _branch);
                await delete.DeleteLocalAsync();
            }

            return true;
        }

        private async Task<bool> FinishHotfixAsync()
        {
            var hotfixName = GetBranchName();

            // Checkout master
            var checkout = new Checkout(_repo);
            if (!await checkout.BranchAsync(_config.Master, false))
                return false;

            // Merge hotfix to master
            var mergeMaster = new Merge(_repo, _branch, "", false);
            if (!await mergeMaster.ExecAsync())
                return false;

            // Tag the hotfix
            var tag = new Tag(_repo, $"hotfix-{hotfixName}");
            await tag.AddAsync(_config.Master, $"Hotfix {hotfixName}", false);

            // Checkout develop
            if (!await checkout.BranchAsync(_config.Develop, false))
                return false;

            // Merge hotfix to develop
            var mergeDevelop = new Merge(_repo, _branch, "", false);
            if (!await mergeDevelop.ExecAsync())
                return false;

            // Delete hotfix branch if not keeping
            if (!_keepBranch)
            {
                var delete = new Branch(_repo, _branch);
                await delete.DeleteLocalAsync();
            }

            return true;
        }

        private string GetBranchName()
        {
            return _config.GetDisplayName(_branch, _type);
        }
    }
}