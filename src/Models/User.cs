using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SourceGit.Models
{
    public class User
    {
        public static readonly User Invalid = new User();
        private const int MAX_CACHE_SIZE = 5000; // Limit cache size
        private const int CACHE_CLEANUP_SIZE = 1000; // Remove this many when cleaning

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public User()
        {
            // Only used by User.Invalid
        }

        public User(string data)
        {
            var parts = data.Split('±', 2);
            if (parts.Length < 2)
                parts = [string.Empty, data];

            Name = parts[0];
            Email = parts[1].TrimStart('<').TrimEnd('>');
            _hash = data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is User other && Name == other.Name && Email == other.Email;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public static User FindOrAdd(string data)
        {
            // Check cache size and cleanup if needed
            if (_caches.Count > MAX_CACHE_SIZE)
            {
                CleanupCache();
            }
            
            return _caches.GetOrAdd(data, key => new User(key));
        }

        public override string ToString()
        {
            return $"{Name} <{Email}>";
        }

        /// <summary>
        /// Clean up the user cache to prevent unbounded growth
        /// </summary>
        public static void CleanupCache()
        {
            if (_caches.Count <= MAX_CACHE_SIZE / 2)
                return;

            // Remove least recently used entries
            // Since ConcurrentDictionary doesn't track access time, we'll remove oldest entries
            var toRemove = _caches.Keys.Take(CACHE_CLEANUP_SIZE).ToList();
            foreach (var key in toRemove)
            {
                _caches.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Clear the entire cache (for memory pressure situations)
        /// </summary>
        public static void ClearCache()
        {
            _caches.Clear();
        }

        /// <summary>
        /// Get current cache size for monitoring
        /// </summary>
        public static int GetCacheSize()
        {
            return _caches.Count;
        }

        private static readonly ConcurrentDictionary<string, User> _caches = new ConcurrentDictionary<string, User>();
        private readonly int _hash;
    }
}
