using SoundSwitcher.Domain;

namespace SoundSwitcher.Tests.Domain;

public sealed class AudioDeviceTests
{
    [Fact]
    public void ConstructorStoresEndpointIdentityAndAvailability()
    {
        var device = new AudioDevice("endpoint-1", "Desk Speakers", isAvailable: true);

        Assert.Equal("endpoint-1", device.EndpointId);
        Assert.Equal("Desk Speakers", device.DisplayName);
        Assert.True(device.IsAvailable);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsMissingEndpointId(string endpointId)
    {
        Assert.Throws<ArgumentException>(() => new AudioDevice(endpointId, "Desk Speakers", isAvailable: true));
    }
}
