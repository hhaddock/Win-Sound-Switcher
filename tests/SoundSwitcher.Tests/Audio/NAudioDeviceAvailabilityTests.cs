using SoundSwitcher.Audio;

namespace SoundSwitcher.Tests.Audio;

public sealed class NAudioDeviceAvailabilityTests
{
    [Fact]
    public async Task LooksUpAnActiveOutputByStableEndpointId()
    {
        var service = new NAudioDeviceService();
        var activeDevice = (await service.GetOutputDevicesAsync()).First();

        var resolvedDevice = await service.GetOutputDeviceAsync(activeDevice.EndpointId);

        Assert.NotNull(resolvedDevice);
        Assert.Equal(activeDevice.EndpointId, resolvedDevice.EndpointId);
        Assert.True(resolvedDevice.IsAvailable);
    }

    [Fact]
    public async Task ReturnsNullForAnEndpointWindowsDoesNotKnow()
    {
        var service = new NAudioDeviceService();

        var resolvedDevice = await service.GetOutputDeviceAsync(
            "{sound-switcher-test-endpoint-that-does-not-exist}");

        Assert.Null(resolvedDevice);
    }
}
