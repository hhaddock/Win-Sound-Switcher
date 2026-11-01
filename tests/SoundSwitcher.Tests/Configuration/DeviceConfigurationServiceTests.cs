using SoundSwitcher.Configuration;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Tests.Configuration;

public sealed class DeviceConfigurationServiceTests
{
    private readonly DeviceConfigurationService _service = new();

    [Fact]
    public void KeepsConfiguredOrderAndAppendsNewlyDiscoveredDevices()
    {
        var configured = new[]
        {
            new ConfiguredAudioDevice("headphones", "Headphones"),
            new ConfiguredAudioDevice("missing", "Travel headset")
        };
        var discovered = new[]
        {
            new AudioDevice("speakers", "Desk speakers", isAvailable: true),
            new AudioDevice("headphones", "USB headphones", isAvailable: true)
        };

        var items = _service.Merge(configured, discovered);

        Assert.Equal(["headphones", "missing", "speakers"], items.Select(item => item.EndpointId));
        Assert.True(items[0].IsAvailable);
        Assert.False(items[1].IsAvailable);
        Assert.Equal("Travel headset", items[1].Label);
        Assert.True(items[2].IsEnabled);
        Assert.Equal("Desk speakers", items[2].Label);
    }

    [Fact]
    public void CreatesSettingsWithEditedOrderLabelsAndEnabledState()
    {
        var items = new[]
        {
            new ConfigurationDeviceItem("speakers", "Desk", "Speakers", isEnabled: false, isAvailable: true),
            new ConfigurationDeviceItem("headphones", "Cans", "Headphones", isEnabled: true, isAvailable: true)
        };

        var settings = _service.CreateSettings(
            items,
            HotkeySettings.Default,
            launchAtStartup: true);

        Assert.Equal(["speakers", "headphones"], settings.ConfiguredDevices.Select(device => device.EndpointId));
        Assert.Equal(["Desk", "Cans"], settings.ConfiguredDevices.Select(device => device.Label));
        Assert.False(settings.ConfiguredDevices[0].IsEnabled);
        Assert.True(settings.ConfiguredDevices[1].IsEnabled);
        Assert.True(settings.LaunchAtStartup);
    }

    [Fact]
    public void RejectsBlankEditedLabels()
    {
        var items = new[]
        {
            new ConfigurationDeviceItem("speakers", " ", "Speakers", isEnabled: true, isAvailable: true)
        };

        Assert.Throws<ArgumentException>(() => _service.CreateSettings(
            items,
            HotkeySettings.Default,
            launchAtStartup: false));
    }
}
