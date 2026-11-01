using SoundSwitcher.Domain;

namespace SoundSwitcher.Audio;

/// <summary>
/// Provides access to Windows audio render endpoints without exposing COM or
/// third-party audio-library types to the rest of the application.
/// </summary>
public interface IAudioDeviceService
{
    Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(CancellationToken cancellationToken = default);

    Task<AudioDevice?> GetOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default);

    Task<AudioDevice?> GetDefaultOutputDeviceAsync(CancellationToken cancellationToken = default);

    Task SetDefaultOutputDeviceAsync(string endpointId, CancellationToken cancellationToken = default);
}
