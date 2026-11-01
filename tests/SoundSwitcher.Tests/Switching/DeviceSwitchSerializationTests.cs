using SoundSwitcher.Audio;
using SoundSwitcher.Domain;
using SoundSwitcher.Switching;

namespace SoundSwitcher.Tests.Switching;

public sealed class DeviceSwitchSerializationTests
{
    [Fact]
    public async Task ConcurrentSelectionsAreSerializedAndLeaveTheLastSelectionActive()
    {
        var audio = new BlockingAudioDeviceService(
            new AudioDevice("speakers", "Speakers", isAvailable: true),
            new AudioDevice("headphones", "Headphones", isAvailable: true),
            new AudioDevice("monitor", "Monitor", isAvailable: true))
        {
            CurrentDeviceId = "speakers",
            BlockFirstSet = true
        };
        var service = new DeviceSwitchService(audio);
        var configured = new[]
        {
            new ConfiguredAudioDevice("speakers", "Speakers"),
            new ConfiguredAudioDevice("headphones", "Headphones"),
            new ConfiguredAudioDevice("monitor", "Monitor")
        };

        var firstSelection = service.SelectDeviceAsync(configured, "headphones");
        await audio.FirstSetStarted.Task;

        var secondSelection = service.SelectDeviceAsync(configured, "monitor");

        Assert.False(secondSelection.IsCompleted);

        audio.ReleaseFirstSet.TrySetResult(true);
        await Task.WhenAll(firstSelection, secondSelection);

        Assert.Equal(1, audio.MaximumConcurrentSetOperations);
        Assert.Equal("monitor", audio.CurrentDeviceId);
    }

    [Fact]
    public async Task FailedSelectionReleasesTheGateForTheNextRequest()
    {
        var audio = new BlockingAudioDeviceService(
            new AudioDevice("speakers", "Speakers", isAvailable: true),
            new AudioDevice("headphones", "Headphones", isAvailable: true))
        {
            CurrentDeviceId = "speakers",
            FailNextSet = true
        };
        var service = new DeviceSwitchService(audio);
        var configured = new[]
        {
            new ConfiguredAudioDevice("speakers", "Speakers"),
            new ConfiguredAudioDevice("headphones", "Headphones")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectDeviceAsync(configured, "headphones"));

        var selected = await service.SelectDeviceAsync(configured, "headphones");

        Assert.Equal("headphones", selected?.EndpointId);
        Assert.Equal(1, audio.MaximumConcurrentSetOperations);
    }

    private sealed class BlockingAudioDeviceService : IAudioDeviceService
    {
        private readonly Dictionary<string, AudioDevice> _devices;
        private int _activeSetOperations;
        private int _setOperationCount;

        public BlockingAudioDeviceService(params AudioDevice[] devices)
        {
            _devices = devices.ToDictionary(
                device => device.EndpointId,
                StringComparer.OrdinalIgnoreCase);
        }

        public string? CurrentDeviceId { get; set; }

        public bool FailNextSet { get; set; }

        public bool BlockFirstSet { get; set; }

        public TaskCompletionSource<bool> FirstSetStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleaseFirstSet { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int MaximumConcurrentSetOperations { get; private set; }

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

        public async Task SetDefaultOutputDeviceAsync(
            string endpointId,
            CancellationToken cancellationToken = default)
        {
            var activeOperations = Interlocked.Increment(ref _activeSetOperations);
            MaximumConcurrentSetOperations = Math.Max(MaximumConcurrentSetOperations, activeOperations);

            try
            {
                if (BlockFirstSet && Interlocked.Increment(ref _setOperationCount) == 1)
                {
                    FirstSetStarted.TrySetResult(true);
                    await ReleaseFirstSet.Task;
                }

                if (FailNextSet)
                {
                    FailNextSet = false;
                    throw new InvalidOperationException("Simulated switch failure.");
                }

                CurrentDeviceId = endpointId;
            }
            finally
            {
                Interlocked.Decrement(ref _activeSetOperations);
            }
        }
    }
}
