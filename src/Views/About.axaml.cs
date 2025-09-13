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

            // Use hardcoded Iniationware version for now
            TxtVersion.Text = "2025.34-IW.4";

            var assembly = Assembly.GetExecutingAssembly();
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (copyright != null)
                TxtCopyright.Text = copyright.Copyright;
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
