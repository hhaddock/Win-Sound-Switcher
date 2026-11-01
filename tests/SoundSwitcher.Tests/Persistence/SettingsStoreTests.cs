using SoundSwitcher.Domain;
using SoundSwitcher.Persistence;

namespace SoundSwitcher.Tests.Persistence;

public sealed class SettingsStoreTests
{
    [Fact]
    public async Task MissingFileReturnsFirstRunDefaults()
    {
        using var sandbox = TestSandbox.Create();
        var store = new SettingsStore(sandbox.FilePath);

        var settings = await store.LoadAsync();

        Assert.Empty(settings.ConfiguredDevices);
        Assert.Equal(HotkeySettings.Default, settings.Hotkey);
        Assert.False(settings.LaunchAtStartup);
    }

    [Fact]
    public async Task SavesAndLoadsSettingsWithoutChangingDeviceOrder()
    {
        using var sandbox = TestSandbox.Create();
        var store = new SettingsStore(sandbox.FilePath);
        var expected = new SoundSwitcherSettings(
        [
            new ConfiguredAudioDevice("headphones", "Headphones"),
            new ConfiguredAudioDevice("speakers", "Desk speakers", isEnabled: false)
        ],
        new HotkeySettings(0x48, HotkeyModifiers.Control | HotkeyModifiers.Shift),
        launchAtStartup: true);

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.Hotkey, actual.Hotkey);
        Assert.Equal(expected.LaunchAtStartup, actual.LaunchAtStartup);
        Assert.Equal(
            expected.ConfiguredDevices.Select(device => device.EndpointId),
            actual.ConfiguredDevices.Select(device => device.EndpointId));
        Assert.Equal(
            expected.ConfiguredDevices.Select(device => device.Label),
            actual.ConfiguredDevices.Select(device => device.Label));
        Assert.Equal(
            expected.ConfiguredDevices.Select(device => device.IsEnabled),
            actual.ConfiguredDevices.Select(device => device.IsEnabled));
        Assert.True(File.Exists(sandbox.FilePath));
    }

    [Fact]
    public async Task MalformedFileIsBackedUpAndReplacedWithDefaults()
    {
        using var sandbox = TestSandbox.Create();
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(sandbox.FilePath)!);
        await File.WriteAllTextAsync(sandbox.FilePath, "{ this is not valid json");
        var store = new SettingsStore(sandbox.FilePath);

        var settings = await store.LoadAsync();

        Assert.Empty(settings.ConfiguredDevices);
        Assert.False(File.Exists(sandbox.FilePath));
        Assert.Single(System.IO.Directory.GetFiles(
            Path.GetDirectoryName(sandbox.FilePath)!,
            "settings.json.invalid-*"));
    }

    private sealed class TestSandbox : IDisposable
    {
        private TestSandbox(string directory)
        {
            Directory = directory;
            FilePath = Path.Combine(directory, "settings.json");
        }

        public string Directory { get; }

        public string FilePath { get; }

        public static TestSandbox Create()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "SoundSwitcherTests",
                Guid.NewGuid().ToString("N"));

            return new TestSandbox(directory);
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }
}
