using Microsoft.Win32;

namespace SoundSwitcher.Startup;

/// <summary>
/// Manages the current-user Windows Run entry for the portable application.
/// </summary>
public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SoundSwitcher";

    public void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (runKey is null)
        {
            throw new InvalidOperationException("Windows startup registration could not be opened.");
        }

        if (!enabled)
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("The application executable path is unavailable.");
        }

        runKey.SetValue(ValueName, QuoteExecutablePath(executablePath));
    }

    public bool IsEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return runKey?.GetValue(ValueName) is string value
            && !string.IsNullOrWhiteSpace(value);
    }

    public static string QuoteExecutablePath(string executablePath) =>
        $"\"{executablePath}\"";
}
