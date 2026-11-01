using System.Text.Json.Serialization;

namespace SoundSwitcher.Domain;

/// <summary>
/// A Windows audio render endpoint discovered by the audio infrastructure.
/// </summary>
public sealed record AudioDevice
{
    public AudioDevice(string endpointId, string displayName, bool isAvailable)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            throw new ArgumentException("An audio device must have an endpoint ID.", nameof(endpointId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("An audio device must have a display name.", nameof(displayName));
        }

        EndpointId = endpointId;
        DisplayName = displayName;
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// The stable identifier assigned to the endpoint by Windows.
    /// </summary>
    [JsonIgnore]
    public string EndpointId { get; }

    /// <summary>
    /// The current display name reported by Windows.
    /// </summary>
    [JsonIgnore]
    public string DisplayName { get; }

    [JsonIgnore]
    public bool IsAvailable { get; }
}
