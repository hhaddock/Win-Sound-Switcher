using SoundSwitcher.Startup;

namespace SoundSwitcher.Tests.Startup;

public sealed class WindowsStartupRegistrationServiceTests
{
    [Fact]
    public void QuotesExecutablePathsForRunKeyValues()
    {
        var quoted = WindowsStartupRegistrationService.QuoteExecutablePath(
            @"C:\Program Files\Sound Switcher\SoundSwitcher.exe");

        Assert.Equal(
            "\"C:\\Program Files\\Sound Switcher\\SoundSwitcher.exe\"",
            quoted);
    }
}
