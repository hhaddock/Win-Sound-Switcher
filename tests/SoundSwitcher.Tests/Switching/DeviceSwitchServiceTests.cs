using SoundSwitcher.Audio;
using SoundSwitcher.Domain;
using SoundSwitcher.Switching;

namespace SoundSwitcher.Tests.Switching;

public sealed class DeviceSwitchServiceTests
{
    [Fact]
    public async Task SelectsConfiguredDeviceWithOneWorkerOperation()
    {
        var audio = new FakeAudioDeviceService(
            new AudioDevice("speakers", "Speakers", isAvailable: true),
            new AudioDevice("headphones", "Headphones", isAvailable: true))
        {
            CurrentDeviceId = "speakers"
        };
        var service = new DeviceSwitchService(audio);

        var selected = await service.SelectDeviceAsync(
            ConfiguredDevices("speakers", "headphones"),
            "headphones");

        Assert.Equal("headphones", selected?.EndpointId);
        Assert.Equal(new[] { "headphones" }, audio.SetDefaultCalls);
    }

    [Fact]
    public async Task DoesNotSelectAnUnconfiguredOrDisabledDevice()
    {
        var audio = new FakeAudioDeviceService(
            new AudioDevice("speakers", "Speakers", isAvailable: true),
            new AudioDevice("headphones", "Headphones", isAvailable: false));
        var service = new DeviceSwitchService(audio);
        var configured = new[]
        {
            new ConfiguredAudioDevice("speakers", "Speakers"),
            new ConfiguredAudioDevice("headphones", "Headphones"),
            new ConfiguredAudioDevice("monitor", "Monitor", isEnabled: false)
        };

        Assert.Null(await service.SelectDeviceAsync(configured, "monitor"));
        Assert.Null(await service.SelectDeviceAsync(configured, "unknown"));
        Assert.Empty(audio.SetDefaultCalls);
    }

    [Fact]
    public async Task ReselectingTheCurrentDeviceUsesTheSingleWorkerOperation()
    {
        var audio = new FakeAudioDeviceService(
            new AudioDevice("speakers", "Speakers", isAvailable: true))
        {
            CurrentDeviceId = "speakers"
        };
        var service = new DeviceSwitchService(audio);

        var selected = await service.SelectDeviceAsync(
            ConfiguredDevices("speakers"),
            "SPEAKERS");

        Assert.Equal("speakers", selected?.EndpointId);
        Assert.Equal(new[] { "speakers" }, audio.SetDefaultCalls);
    }

    private static ConfiguredAudioDevice[] ConfiguredDevices(params string[] endpointIds) =>
        endpointIds
            .Select(endpointId => new ConfiguredAudioDevice(endpointId, endpointId))
            .ToArray();

    private sealed class FakeAudioDeviceService : IAudioDeviceService
    {
        private readonly Dictionary<string, AudioDevice> _devices;

        public FakeAudioDeviceService(params AudioDevice[] devices)
        {
            _devices = devices.ToDictionary(
                device => device.EndpointId,
                StringComparer.OrdinalIgnoreCase);
        }

        public string? CurrentDeviceId { get; set; }

        public List<string> SetDefaultCalls { get; } = [];

        public Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AudioDevice>>(_devices.Values.ToArray());

        public Task<AudioDevice?> GetOutputDeviceAsync(
            string endpointId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_devices.TryGetValue(endpointId, out var device) ? device : null);

        public Task<AudioDevice?> GetDefaultOutputDeviceAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                CurrentDeviceId is not null && _devices.TryGetValue(CurrentDeviceId, out var device)
                    ? device
                    : null);

        public Task SetDefaultOutputDeviceAsync(
            string endpointId,
            CancellationToken cancellationToken = default)
        {
            SetDefaultCalls.Add(endpointId);
            CurrentDeviceId = endpointId;
            return Task.CompletedTask;
        }
    }
}
