using SoundSwitcher.Overlay;

namespace SoundSwitcher.Tests.Overlay;

public sealed class SwitcherDeviceItemTests
{
    [Fact]
    public void ActiveDeviceReportsActiveStatus()
    {
        var item = new SwitcherDeviceItem("speakers", "Speakers", true, true);

        Assert.Equal("Active", item.StatusText);
    }

    [Fact]
    public void MissingDeviceReportsUnavailableStatus()
    {
        var item = new SwitcherDeviceItem("headphones", "Headphones", false, false);

        Assert.Equal("Unavailable", item.StatusText);
    }
}
