using SoundSwitcher.Audio;
using SoundSwitcher.Domain;
using System.ComponentModel;

namespace SoundSwitcher.Tests.Audio;

public sealed class IsolatedAudioDeviceServiceTests
{
    [Fact]
    public async Task ReportsWorkerFailureWithoutThrowingFromTheParentProcess()
    {
        var service = new IsolatedAudioDeviceService(
            Path.Combine(
                Path.GetTempPath(),
                $"sound-switcher-missing-worker-{Guid.NewGuid():N}.exe"));

        await Assert.ThrowsAsync<Win32Exception>(() =>
            service.SetDefaultOutputDeviceAsync("endpoint"));
    }

}
