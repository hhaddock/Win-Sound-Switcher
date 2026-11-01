using System.Text.Json.Serialization;

namespace SoundSwitcher.Domain;

/// <summary>
/// Persisted user preferences for Sound Switcher.
/// </summary>
public sealed record SoundSwitcherSettings
{
    public SoundSwitcherSettings(
        IReadOnlyList<ConfiguredAudioDevice> configuredDevices,
        HotkeySettings? hotkey = null,
        bool launchAtStartup = false,
        int schemaVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(configuredDevices);

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion), "The settings schema version must be positive.");
        }

        ConfiguredDevices = configuredDevices.ToArray();
        Hotkey = hotkey ?? HotkeySettings.Default;
        LaunchAtStartup = launchAtStartup;
        SchemaVersion = schemaVersion;
    }

    [JsonPropertyName("devices")]
    public IReadOnlyList<ConfiguredAudioDevice> ConfiguredDevices { get; }

    [JsonPropertyName("hotkey")]
    public HotkeySettings Hotkey { get; }

    [JsonPropertyName("launchAtStartup")]
    public bool LaunchAtStartup { get; }

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; }
}
