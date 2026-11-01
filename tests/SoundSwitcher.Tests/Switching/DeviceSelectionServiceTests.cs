using SoundSwitcher.Domain;
using SoundSwitcher.Switching;

namespace SoundSwitcher.Tests.Switching;

public sealed class DeviceSelectionServiceTests
{
    private readonly DeviceSelectionService _selector = new();

    [Fact]
    public void CyclesThroughConfiguredDevicesInOrder()
    {
        var devices = ConfiguredDevices("speakers", "headphones", "monitor");
        var available = ActiveDevices("speakers", "headphones", "monitor");

        var next = _selector.SelectNext(devices, available, "headphones");

        Assert.Equal("monitor", next?.EndpointId);
    }

    [Fact]
    public void WrapsFromTheLastDeviceToTheFirst()
    {
        var devices = ConfiguredDevices("speakers", "headphones", "monitor");
        var available = ActiveDevices("speakers", "headphones", "monitor");

        var next = _selector.SelectNext(devices, available, "monitor");

        Assert.Equal("speakers", next?.EndpointId);
    }

    [Fact]
    public void CyclesBetweenTwoConfiguredDevices()
    {
        var devices = ConfiguredDevices("speakers", "headphones");
        var available = ActiveDevices("speakers", "headphones");

        var next = _selector.SelectNext(devices, available, "speakers");

        Assert.Equal("headphones", next?.EndpointId);
    }

    [Fact]
    public void SkipsDisabledAndUnavailableDevices()
    {
        var devices =
        new[]
        {
            new ConfiguredAudioDevice("speakers", "Speakers"),
            new ConfiguredAudioDevice("headphones", "Headphones", isEnabled: false),
            new ConfiguredAudioDevice("monitor", "Monitor")
        };
        var available = ActiveDevices("speakers", "headphones");

        var next = _selector.SelectNext(devices, available, "speakers");

        Assert.Null(next);
    }

    [Fact]
    public void SelectsTheFirstEligibleDeviceWhenCurrentIsNotConfigured()
    {
        var devices = ConfiguredDevices("speakers", "headphones", "monitor");
        var available = ActiveDevices("speakers", "headphones", "monitor");

        var next = _selector.SelectNext(devices, available, "unknown-device");

        Assert.Equal("speakers", next?.EndpointId);
    }

    [Fact]
    public void ReturnsNullWhenNoConfiguredDeviceIsAvailable()
    {
        var devices = ConfiguredDevices("speakers", "headphones");
        var available = ActiveDevices("monitor");

        var next = _selector.SelectNext(devices, available, "monitor");

        Assert.Null(next);
    }

    [Fact]
    public void ReturnsNullWhenTheOnlyEligibleDeviceIsAlreadyCurrent()
    {
        var devices = ConfiguredDevices("speakers");
        var available = ActiveDevices("speakers");

        var next = _selector.SelectNext(devices, available, "speakers");

        Assert.Null(next);
    }

    private static ConfiguredAudioDevice[] ConfiguredDevices(params string[] endpointIds) =>
        endpointIds
            .Select(endpointId => new ConfiguredAudioDevice(endpointId, endpointId))
            .ToArray();

    private static AudioDevice[] ActiveDevices(params string[] endpointIds) =>
        endpointIds
            .Select(endpointId => new AudioDevice(endpointId, endpointId, isAvailable: true))
            .ToArray();
}
