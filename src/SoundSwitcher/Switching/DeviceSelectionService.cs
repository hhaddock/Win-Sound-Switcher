using SoundSwitcher.Domain;

namespace SoundSwitcher.Switching;

/// <summary>
/// Selects the next configured output without performing the Windows switch.
/// </summary>
public sealed class DeviceSelectionService
{
    public ConfiguredAudioDevice? SelectNext(
        IReadOnlyList<ConfiguredAudioDevice> configuredDevices,
        IReadOnlyCollection<AudioDevice> availableDevices,
        string? currentEndpointId)
    {
        ArgumentNullException.ThrowIfNull(configuredDevices);
        ArgumentNullException.ThrowIfNull(availableDevices);

        var availableEndpointIds = availableDevices
            .Where(device => device.IsAvailable)
            .Select(device => device.EndpointId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var eligibleDevices = configuredDevices
            .Where(device => device.IsEnabled && availableEndpointIds.Contains(device.EndpointId))
            .ToArray();

        if (eligibleDevices.Length == 0)
        {
            return null;
        }

        var currentIndex = string.IsNullOrWhiteSpace(currentEndpointId)
            ? -1
            : FindDeviceIndex(configuredDevices, currentEndpointId);

        if (currentIndex < 0)
        {
            return eligibleDevices[0];
        }

        for (var offset = 1; offset < configuredDevices.Count; offset++)
        {
            var candidate = configuredDevices[(currentIndex + offset) % configuredDevices.Count];

            if (candidate.IsEnabled && availableEndpointIds.Contains(candidate.EndpointId))
            {
                return candidate;
            }
        }

        // The only eligible device is already active, so there is nowhere to switch.
        return null;
    }

    private static int FindDeviceIndex(
        IReadOnlyList<ConfiguredAudioDevice> configuredDevices,
        string endpointId)
    {
        for (var index = 0; index < configuredDevices.Count; index++)
        {
            if (string.Equals(
                    configuredDevices[index].EndpointId,
                    endpointId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}
