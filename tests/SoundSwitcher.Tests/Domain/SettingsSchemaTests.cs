using System.Text.Json;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Tests.Domain;

public sealed class SettingsSchemaTests
{
    [Fact]
    public void SerializesTheStableSettingsShape()
    {
        var settings = new SoundSwitcherSettings(
        [
            new ConfiguredAudioDevice("speakers", "Speakers", isEnabled: true),
            new ConfiguredAudioDevice("headphones", "Headphones", isEnabled: false)
        ]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(settings));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("schemaVersion", out _));
        Assert.True(root.TryGetProperty("devices", out var devices));
        Assert.True(root.TryGetProperty("hotkey", out var hotkey));
        Assert.True(root.TryGetProperty("launchAtStartup", out _));
        Assert.Equal(JsonValueKind.Array, devices.ValueKind);
        Assert.Equal(2, devices.GetArrayLength());
        Assert.True(devices[0].TryGetProperty("endpointId", out _));
        Assert.True(devices[0].TryGetProperty("label", out _));
        Assert.True(devices[0].TryGetProperty("isEnabled", out _));
        Assert.True(hotkey.TryGetProperty("virtualKey", out _));
        Assert.True(hotkey.TryGetProperty("modifiers", out _));
        Assert.DoesNotContain("isAvailable", document.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("displayName", document.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
