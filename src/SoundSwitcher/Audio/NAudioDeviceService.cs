using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Audio;

/// <summary>
/// Enumerates Windows Core Audio render endpoints through NAudio's WASAPI adapter.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NAudioDeviceService : IAudioDeviceService
{
    public Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var enumerator = new MMDeviceEnumerator();
        using var endpointCollection = enumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        var devices = new List<AudioDevice>(endpointCollection.Count);

        foreach (var endpoint in endpointCollection)
        {
            using (endpoint)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var displayName = string.IsNullOrWhiteSpace(endpoint.FriendlyName)
                    ? endpoint.ID
                    : endpoint.FriendlyName;

                devices.Add(new AudioDevice(endpoint.ID, displayName, isAvailable: true));
            }
        }

        return Task.FromResult<IReadOnlyList<AudioDevice>>(devices);
    }

    public Task<AudioDevice?> GetOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            throw new ArgumentException("An endpoint ID is required.", nameof(endpointId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var endpoint = enumerator.GetDevice(endpointId);

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<AudioDevice?>(CreateAudioDevice(endpoint));
        }
        catch (COMException)
        {
            // Windows no longer knows this endpoint ID. It may have been removed
            // rather than temporarily unplugged.
            return Task.FromResult<AudioDevice?>(null);
        }
    }

    public Task<AudioDevice?> GetDefaultOutputDeviceAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var enumerator = new MMDeviceEnumerator();

        if (!enumerator.TryGetDefaultAudioEndpoint(
                DataFlow.Render,
                Role.Multimedia,
                out var endpoint)
            || endpoint is null)
        {
            return Task.FromResult<AudioDevice?>(null);
        }

        using (endpoint)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<AudioDevice?>(CreateAudioDevice(endpoint));
        }
    }

    public Task SetDefaultOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            throw new ArgumentException("An endpoint ID is required.", nameof(endpointId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        using (var enumerator = new MMDeviceEnumerator())
        using (var endpoint = enumerator.GetDevice(endpointId))
        {
            if (endpoint.State != DeviceState.Active)
            {
                throw new InvalidOperationException("The requested audio device is not active.");
            }
        }

        AudioPolicyConfig.SetDefaultOutputEndpoint(endpointId, cancellationToken);
        return Task.CompletedTask;
    }

    private static AudioDevice CreateAudioDevice(MMDevice endpoint)
    {
        var displayName = string.IsNullOrWhiteSpace(endpoint.FriendlyName)
            ? endpoint.ID
            : endpoint.FriendlyName;

        return new AudioDevice(
            endpoint.ID,
            displayName,
            isAvailable: endpoint.State == DeviceState.Active);
    }
}
