namespace SoundSwitcher.Audio;

/// <summary>
/// JSON-safe audio device data exchanged with the isolated audio worker.
/// </summary>
public sealed record AudioDeviceWireModel(
    string EndpointId,
    string DisplayName,
    bool IsAvailable);
