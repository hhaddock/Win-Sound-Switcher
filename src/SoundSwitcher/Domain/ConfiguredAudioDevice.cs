using System.Text.Json.Serialization;

namespace SoundSwitcher.Domain;

/// <summary>
/// A user-selected audio endpoint and its switcher preferences.
/// </summary>
public sealed record ConfiguredAudioDevice
{
    public ConfiguredAudioDevice(string endpointId, string label, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            throw new ArgumentException("A configured device must have an endpoint ID.", nameof(endpointId));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("A configured device must have a label.", nameof(label));
        }

        EndpointId = endpointId;
        Label = label;
        IsEnabled = isEnabled;
    }

    [JsonPropertyName("endpointId")]
    public string EndpointId { get; }

    [JsonPropertyName("label")]
    public string Label { get; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; }
}
