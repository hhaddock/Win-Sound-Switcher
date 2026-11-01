using SoundSwitcher.Audio;

namespace SoundSwitcher.Tests.Audio;

public sealed class NAudioDefaultDeviceTests
{
    [Fact]
    public async Task ReadsTheCurrentDefaultOutputDevice()
    {
        var service = new NAudioDeviceService();

        var defaultDevice = await service.GetDefaultOutputDeviceAsync();

        Assert.NotNull(defaultDevice);
        Assert.False(string.IsNullOrWhiteSpace(defaultDevice.EndpointId));
        Assert.True(defaultDevice.IsAvailable);
    }

    [Fact]
    public async Task SetsAnOutputDeviceAndRestoresTheOriginalDevice()
    {
        var service = new NAudioDeviceService();
        var devices = await service.GetOutputDevicesAsync();
        var originalDevice = await service.GetDefaultOutputDeviceAsync();

        Assert.NotNull(originalDevice);

        var alternateDevice = devices.FirstOrDefault(device =>
            !string.Equals(device.EndpointId, originalDevice.EndpointId, StringComparison.OrdinalIgnoreCase));

        if (alternateDevice is null)
        {
            return;
        }

        try
        {
            await service.SetDefaultOutputDeviceAsync(alternateDevice.EndpointId);

            var currentDevice = await service.GetDefaultOutputDeviceAsync();

            Assert.NotNull(currentDevice);
            Assert.Equal(alternateDevice.EndpointId, currentDevice.EndpointId);
        }
        finally
        {
            await service.SetDefaultOutputDeviceAsync(originalDevice.EndpointId);
        }
    }
}
