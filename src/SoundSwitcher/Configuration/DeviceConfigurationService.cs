using SoundSwitcher.Domain;

namespace SoundSwitcher.Configuration;

public sealed class DeviceConfigurationService
{
    public IReadOnlyList<ConfigurationDeviceItem> Merge(
        IReadOnlyList<ConfiguredAudioDevice> configuredDevices,
        IReadOnlyList<AudioDevice> discoveredDevices)
    {
        ArgumentNullException.ThrowIfNull(configuredDevices);
        ArgumentNullException.ThrowIfNull(discoveredDevices);

        var discoveredById = discoveredDevices.ToDictionary(
            device => device.EndpointId,
            StringComparer.OrdinalIgnoreCase);
        var configuredIds = configuredDevices
            .Select(device => device.EndpointId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<ConfigurationDeviceItem>(configuredDevices.Count + discoveredDevices.Count);

        foreach (var configuredDevice in configuredDevices)
        {
            if (discoveredById.TryGetValue(configuredDevice.EndpointId, out var discoveredDevice))
            {
                result.Add(new ConfigurationDeviceItem(
                    configuredDevice.EndpointId,
                    configuredDevice.Label,
                    discoveredDevice.DisplayName,
                    configuredDevice.IsEnabled,
                    isAvailable: true));
            }
            else
            {
                result.Add(new ConfigurationDeviceItem(
                    configuredDevice.EndpointId,
                    configuredDevice.Label,
                    configuredDevice.Label,
                    configuredDevice.IsEnabled,
                    isAvailable: false));
            }
        }

        foreach (var discoveredDevice in discoveredDevices)
        {
            if (configuredIds.Contains(discoveredDevice.EndpointId))
            {
                continue;
            }

            result.Add(new ConfigurationDeviceItem(
                discoveredDevice.EndpointId,
                discoveredDevice.DisplayName,
                discoveredDevice.DisplayName,
                isEnabled: true,
                isAvailable: true));
        }

        return result;
    }

    public SoundSwitcherSettings CreateSettings(
        IEnumerable<ConfigurationDeviceItem> items,
        HotkeySettings hotkey,
        bool launchAtStartup,
        int schemaVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(hotkey);

        var devices = items.Select(item =>
        {
            if (string.IsNullOrWhiteSpace(item.Label))
            {
                throw new ArgumentException(
                    "Every configured device must have a label.",
                    nameof(items));
            }

            return new ConfiguredAudioDevice(
                item.EndpointId,
                item.Label.Trim(),
                item.IsEnabled);
        });

        return new SoundSwitcherSettings(
            devices.ToArray(),
            hotkey,
            launchAtStartup,
            schemaVersion);
    }
}
