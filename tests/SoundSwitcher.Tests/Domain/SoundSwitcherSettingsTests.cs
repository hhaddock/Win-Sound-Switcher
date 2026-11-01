using SoundSwitcher.Domain;

namespace SoundSwitcher.Tests.Domain;

public sealed class SoundSwitcherSettingsTests
{
    [Fact]
    public void ConstructorPreservesConfiguredDeviceOrder()
    {
        var headphones = new ConfiguredAudioDevice("headphones", "Headphones");
        var speakers = new ConfiguredAudioDevice("speakers", "Speakers");

        var settings = new SoundSwitcherSettings([headphones, speakers]);

        Assert.Equal([headphones, speakers], settings.ConfiguredDevices);
    }

    [Fact]
    public void ConstructorUsesAgreedDefaultHotkey()
    {
        var settings = new SoundSwitcherSettings([]);

        Assert.Equal(0x53, settings.Hotkey.VirtualKey);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.Hotkey.Modifiers);
        Assert.False(settings.LaunchAtStartup);
        Assert.Equal(1, settings.SchemaVersion);
    }

    [Fact]
    public void ConstructorCopiesTheDeviceSequence()
    {
        var devices = new List<ConfiguredAudioDevice>
        {
            new("speakers", "Speakers")
        };

        var settings = new SoundSwitcherSettings(devices);
        devices.Add(new ConfiguredAudioDevice("headphones", "Headphones"));

        Assert.Single(settings.ConfiguredDevices);
    }
}
