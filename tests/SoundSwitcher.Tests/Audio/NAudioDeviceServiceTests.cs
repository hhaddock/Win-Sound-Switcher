using SoundSwitcher.Audio;

namespace SoundSwitcher.Tests.Audio;

public sealed class NAudioDeviceServiceTests
{
    [Fact]
    public async Task EnumeratesActiveOutputDevicesOnWindows()
    {
        var service = new NAudioDeviceService();

        var devices = await service.GetOutputDevicesAsync();

        Assert.NotEmpty(devices);
        Assert.All(devices, device =>
        {
            Assert.False(string.IsNullOrWhiteSpace(device.EndpointId));
            Assert.False(string.IsNullOrWhiteSpace(device.DisplayName));
            Assert.True(device.IsAvailable);
        });
    }
}
