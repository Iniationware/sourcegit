using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.Models
{
    /// <summary>
    /// Simple branch counter model that uses real Git data
    /// </summary>
    public class BranchCounter : ObservableObject
    {
        private int _totalBranches;
        private int _localBranches;
        private int _remoteBranches;
        private Dictionary<string, int> _branchTypes = new Dictionary<string, int>();
        private DateTime _lastUpdated;
        private string _errorMessage;

        public int TotalBranches
        {
            get => _totalBranches;
            set => SetProperty(ref _totalBranches, value);
        }

        public int LocalBranches
        {
            get => _localBranches;
            set => SetProperty(ref _localBranches, value);
        }

        public int RemoteBranches
        {
            get => _remoteBranches;
            set => SetProperty(ref _remoteBranches, value);
        }

        public Dictionary<string, int> BranchTypes
        {
            get => _branchTypes;
            set => SetProperty(ref _branchTypes, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Update branch counts from a repository
        /// </summary>
        public void UpdateFromRepository(string repoPath)
        {
            try
            {
                ErrorMessage = null;

                var cmd = new Commands.CountBranches(repoPath);
                var result = cmd.Result();

                if (result != null)
                {
                    TotalBranches = result.Total;
                    LocalBranches = result.Local;
                    RemoteBranches = result.Remote;
                    BranchTypes = result.ByType;
                    LastUpdated = DateTime.Now;
                }
                else
                {
                    ErrorMessage = "Failed to count branches";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                TotalBranches = 0;
                LocalBranches = 0;
                RemoteBranches = 0;
                BranchTypes = new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Get a formatted summary of branch counts
        /// </summary>
        public string GetSummary()
        {
            if (HasError)
                return ErrorMessage;

            var summary = $"Total: {TotalBranches} (Local: {LocalBranches}, Remote: {RemoteBranches})";

            if (BranchTypes.Count > 0)
            {
                summary += "\nTypes: ";
                var types = new List<string>();
                foreach (var kvp in BranchTypes)
                {
                    types.Add($"{kvp.Key}={kvp.Value}");
                }
                summary += string.Join(", ", types);
            }

            return summary;
        }
    }
}
