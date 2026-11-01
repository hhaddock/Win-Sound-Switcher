using SoundSwitcher.Audio;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Switching;

/// <summary>
/// Applies an explicit selection from the configured device list.
/// </summary>
public sealed class DeviceSwitchService
{
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly SemaphoreSlim _switchGate = new(1, 1);

    public DeviceSwitchService(IAudioDeviceService audioDeviceService)
    {
        _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
    }

    /// <summary>
    /// Selects a configured, enabled, and available endpoint.
    /// Returns the requested device after the audio command succeeds, or
    /// null when the endpoint is not configured or enabled.
    /// </summary>
    public async Task<AudioDevice?> SelectDeviceAsync(
        IReadOnlyCollection<ConfiguredAudioDevice> configuredDevices,
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuredDevices);

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            throw new ArgumentException("An endpoint ID is required.", nameof(endpointId));
        }

        await _switchGate.WaitAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var configuredDevice = configuredDevices.FirstOrDefault(device =>
                device.IsEnabled
                && string.Equals(device.EndpointId, endpointId, StringComparison.OrdinalIgnoreCase));

            if (configuredDevice is null)
            {
                return null;
            }

            await _audioDeviceService.SetDefaultOutputDeviceAsync(
                configuredDevice.EndpointId,
                cancellationToken);

            return new AudioDevice(
                configuredDevice.EndpointId,
                configuredDevice.Label,
                isAvailable: true);
        }
        finally
        {
            _switchGate.Release();
        }
    }
}
