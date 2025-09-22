using System;
using System.IO;
using System.Reflection;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class About : ChromelessWindow
    {
        public About()
        {
            CloseOnESC = true;
            InitializeComponent();

            // Get version from VERSION file or assembly
            var version = GetApplicationVersion();
            TxtVersion.Text = $"v{version}";

            var assembly = Assembly.GetExecutingAssembly();
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (copyright != null)
                TxtCopyright.Text = copyright.Copyright;
        }

        private string GetApplicationVersion()
        {
            // First try to read from VERSION file (if it exists in the app directory)
            var versionFile = Path.Combine(AppContext.BaseDirectory, "VERSION");
            if (!File.Exists(versionFile))
            {
                // Try from source directory during development
                var sourceVersionFile = Path.Combine(AppContext.BaseDirectory, "../../../../../VERSION");
                if (File.Exists(sourceVersionFile))
                    versionFile = sourceVersionFile;
            }

            if (File.Exists(versionFile))
            {
                var version = File.ReadAllText(versionFile).Trim();
                if (!string.IsNullOrEmpty(version))
                    return version;
            }

            // Fallback to assembly version
            var assembly = Assembly.GetExecutingAssembly();
            var version_attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (version_attr != null && !string.IsNullOrEmpty(version_attr.InformationalVersion))
                return version_attr.InformationalVersion;

            // Last fallback
            return assembly.GetName().Version?.ToString() ?? "Unknown";
        }

        private void OnVisitReleaseNotes(object _, RoutedEventArgs e)
        {
            Native.OS.OpenBrowser($"https://github.com/Iniationware/sourcegit/releases/tag/{TxtVersion.Text}");
            e.Handled = true;
        }

        private void OnVisitWebsite(object _, RoutedEventArgs e)
        {
            Native.OS.OpenBrowser("https://sourcegit-scm.github.io/");
            e.Handled = true;
        }

        private void OnVisitSourceCode(object _, RoutedEventArgs e)
        {
            Native.OS.OpenBrowser("https://github.com/Iniationware/sourcegit");
            e.Handled = true;
        }
    }
}
