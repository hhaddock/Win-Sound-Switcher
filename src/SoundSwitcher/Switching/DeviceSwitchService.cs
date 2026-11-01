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
    /// Returns the Windows default after the switch, or null when the endpoint
    /// cannot be selected.
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

            var targetDevice = await _audioDeviceService.GetOutputDeviceAsync(
                configuredDevice.EndpointId,
                cancellationToken);

            if (targetDevice is null || !targetDevice.IsAvailable)
            {
                return null;
            }

            var currentDevice = await _audioDeviceService.GetDefaultOutputDeviceAsync(cancellationToken);

            if (currentDevice is not null
                && string.Equals(
                    currentDevice.EndpointId,
                    targetDevice.EndpointId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return currentDevice;
            }

            await _audioDeviceService.SetDefaultOutputDeviceAsync(
                targetDevice.EndpointId,
                cancellationToken);

            return await _audioDeviceService.GetDefaultOutputDeviceAsync(cancellationToken);
        }
        finally
        {
            _switchGate.Release();
        }
    }
}
