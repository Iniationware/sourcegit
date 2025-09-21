using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class GitFlowStartSupport : Popup
    {
        public string Prefix
        {
            get;
            private set;
        }

        [Required(ErrorMessage = "Version name is required!!!")]
        [RegularExpression(@"^[\w\-/\.#]+$", ErrorMessage = "Bad version name format!")]
        [CustomValidation(typeof(GitFlowStartSupport), nameof(ValidateBranchName))]
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value, true);
        }

        public List<Models.Branch> BaseBranches
        {
            get;
            private set;
        }

        [Required(ErrorMessage = "Base branch is required!!!")]
        public Models.Branch SelectedBaseBranch
        {
            get => _selectedBaseBranch;
            set => SetProperty(ref _selectedBaseBranch, value, true);
        }

        public GitFlowStartSupport(Repository repo)
        {
            _repo = repo;
            Prefix = _repo.GitFlow.GetPrefix(Models.GitFlowBranchType.Support);

            // Get list of branches to use as base (typically master/main branches)
            BaseBranches = new List<Models.Branch>();
            foreach (var branch in _repo.Branches)
            {
                if (branch.IsLocal)
                {
                    // Add master/main branches and tagged releases as potential bases
                    if (branch.Name == _repo.GitFlow.Master ||
                        branch.Name.StartsWith(_repo.GitFlow.ReleasePrefix))
                    {
                        BaseBranches.Add(branch);
                    }
                }
            }

            // Default to master branch
            if (BaseBranches.Count > 0)
            {
                foreach (var b in BaseBranches)
                {
                    if (b.Name == _repo.GitFlow.Master)
                    {
                        _selectedBaseBranch = b;
                        break;
                    }
                }
                if (_selectedBaseBranch == null)
                    _selectedBaseBranch = BaseBranches[0];
            }
        }

        public static ValidationResult ValidateBranchName(string version, ValidationContext ctx)
        {
            if (ctx.ObjectInstance is GitFlowStartSupport starter)
            {
                var check = $"{starter.Prefix}{version}";
                foreach (var b in starter._repo.Branches)
                {
                    if (b.FriendlyName == check)
                        return new ValidationResult("A branch with same name already exists!");
                }
            }

            return ValidationResult.Success;
        }

        public override async Task<bool> Sure()
        {
            if (_selectedBaseBranch == null)
                return false;

            _repo.SetWatcherEnabled(false);
            ProgressDescription = $"Git Flow - Start {Prefix}{_version} from {_selectedBaseBranch.Name} ...";

            var log = _repo.CreateLog("GitFlow - Start Support");
            Use(log);

            // For support branches, we need to use a different command format
            // git flow support start <version> <base>
            var cmd = new Commands.Command();
            cmd.WorkingDirectory = _repo.FullPath;
            cmd.Context = _repo.FullPath;
            cmd.Args = $"flow support start {_version} {_selectedBaseBranch.Name}";

            var succ = await cmd.Use(log).ExecAsync().ConfigureAwait(false);
            log.Complete();

            // Refresh branches to show the new Git-Flow branch
            _repo.MarkBranchesDirtyManually();
            _repo.SetWatcherEnabled(true);
            return succ;
        }

        private readonly Repository _repo;
        private string _version = string.Empty;
        private Models.Branch _selectedBaseBranch = null;
    }
}