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
            new StubAudioDeviceService(),
            Path.Combine(
                Path.GetTempPath(),
                $"sound-switcher-missing-worker-{Guid.NewGuid():N}.exe"));

        await Assert.ThrowsAsync<Win32Exception>(() =>
            service.SetDefaultOutputDeviceAsync("endpoint"));
    }

    private sealed class StubAudioDeviceService : IAudioDeviceService
    {
        public Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AudioDevice>>([]);

        public Task<AudioDevice?> GetOutputDeviceAsync(string endpointId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AudioDevice?>(null);

        public Task<AudioDevice?> GetDefaultOutputDeviceAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<AudioDevice?>(null);

        public Task SetDefaultOutputDeviceAsync(string endpointId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
