using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class Commit : Command
    {
        public Commit(string repo, string message, bool signOff, bool noVerify, bool amend, bool resetAuthor)
        {
            _tmpFile = Path.GetTempFileName();
            _message = message;

            WorkingDirectory = repo;
            Context = repo;

            var builder = new StringBuilder();
            builder.Append("commit --allow-empty --file=");
            builder.Append(_tmpFile.Quoted());
            builder.Append(' ');

            if (signOff)
                builder.Append("--signoff ");

            if (noVerify)
                builder.Append("--no-verify ");

            if (amend)
            {
                builder.Append("--amend ");
                if (resetAuthor)
                    builder.Append("--reset-author ");
                builder.Append("--no-edit");
            }

            Args = builder.ToString();
        }

        public async Task<bool> RunAsync()
        {
            try
            {
                await File.WriteAllTextAsync(_tmpFile, _message).ConfigureAwait(false);

                // Store original RaiseError setting to provide custom error handling
                var originalRaiseError = RaiseError;
                RaiseError = false;

                var result = await ReadToEndAsync().ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    // Check for common GPG signing errors and provide helpful messages
                    var errorMessage = result.StdErr;

                    if (errorMessage.Contains("gpg failed to sign", System.StringComparison.OrdinalIgnoreCase) ||
                        errorMessage.Contains("gpg: signing failed", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // GPG signing failed - provide helpful error message
                        var enhancedMessage = "GPG signing failed. Common causes:\n" +
                                            "• GPG key not configured (check 'git config user.signingkey')\n" +
                                            "• GPG agent not running (try 'gpg-agent --daemon')\n" +
                                            "• Passphrase required but no TTY available\n" +
                                            "• Key expired or revoked\n\n" +
                                            "Original error: " + errorMessage;

                        if (originalRaiseError)
                            App.RaiseException(Context, enhancedMessage);
                    }
                    else if (errorMessage.Contains("secret key not available", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var enhancedMessage = "GPG secret key not available.\n" +
                                            "• Check if your GPG key is properly imported\n" +
                                            "• Verify the signing key in git config matches your GPG key\n" +
                                            "• Run 'gpg --list-secret-keys' to see available keys\n\n" +
                                            "Original error: " + errorMessage;

                        if (originalRaiseError)
                            App.RaiseException(Context, enhancedMessage);
                    }
                    else if (errorMessage.Contains("cannot allocate memory", System.StringComparison.OrdinalIgnoreCase) ||
                             errorMessage.Contains("inappropriate ioctl for device", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var enhancedMessage = "GPG TTY error detected.\n" +
                                            "• The GPG_TTY environment variable may not be set correctly\n" +
                                            "• Try running: export GPG_TTY=$(tty)\n" +
                                            "• Or disable commit signing temporarily: git config commit.gpgsign false\n\n" +
                                            "Original error: " + errorMessage;

                        if (originalRaiseError)
                            App.RaiseException(Context, enhancedMessage);
                    }
                    else
                    {
                        // Other errors - show as-is
                        if (originalRaiseError && !string.IsNullOrEmpty(errorMessage))
                            App.RaiseException(Context, errorMessage);
                    }
                }

                File.Delete(_tmpFile);
                return result.IsSuccess;
            }
            catch (System.Exception ex)
            {
                if (File.Exists(_tmpFile))
                    File.Delete(_tmpFile);

                App.RaiseException(Context, $"Commit operation failed: {ex.Message}");
                return false;
            }
        }

        private readonly string _tmpFile = string.Empty;
        private readonly string _message = string.Empty;
    }
}
