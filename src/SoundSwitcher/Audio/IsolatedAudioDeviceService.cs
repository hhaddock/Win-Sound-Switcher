using System.Diagnostics;
using System.Text.Json;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Audio;

/// <summary>
/// Keeps the tray process alive if the Windows Core Audio COM boundary fails.
/// Every operation runs in a short-lived worker process because enumeration and
/// default-device reads can also cross the unstable native boundary.
/// </summary>
public sealed class IsolatedAudioDeviceService : IAudioDeviceService
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _workerPath;

    public IsolatedAudioDeviceService(string workerPath)
    {
        _workerPath = string.IsNullOrWhiteSpace(workerPath)
            ? throw new ArgumentException("An audio worker path is required.", nameof(workerPath))
            : workerPath;
    }

    public async Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var output = await RunWorkerAsync("list", cancellationToken);
        var devices = JsonSerializer.Deserialize<AudioDeviceWireModel[]>(output, JsonOptions) ?? [];
        return devices.Select(ToAudioDevice).ToArray();
    }

    public async Task<AudioDevice?> GetOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        var output = await RunWorkerAsync("get", cancellationToken, endpointId);
        var device = JsonSerializer.Deserialize<AudioDeviceWireModel?>(output, JsonOptions);
        return device is null ? null : ToAudioDevice(device);
    }

    public async Task<AudioDevice?> GetDefaultOutputDeviceAsync(
        CancellationToken cancellationToken = default)
    {
        var output = await RunWorkerAsync("default", cancellationToken);
        var device = JsonSerializer.Deserialize<AudioDeviceWireModel?>(output, JsonOptions);
        return device is null ? null : ToAudioDevice(device);
    }

    public async Task SetDefaultOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        await RunWorkerAsync("switch", cancellationToken, endpointId);
    }

    private async Task<string> RunWorkerAsync(
        string operation,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = _workerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add(operation);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The audio worker could not be started.");

        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).WaitAsync(WorkerTimeout, cancellationToken);

            var output = await outputTask;
            var error = await errorTask;
            if (process.ExitCode != 0)
            {
                var detail = string.IsNullOrWhiteSpace(error)
                    ? $"The audio worker exited with code {process.ExitCode}."
                    : error.Trim();

                throw new InvalidOperationException(detail);
            }

            return output;
        }
        catch
        {
            TryTerminate(process);
            throw;
        }
    }

    private static AudioDevice ToAudioDevice(AudioDeviceWireModel device) =>
        new(device.EndpointId, device.DisplayName, device.IsAvailable);

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The worker exited between the check and Kill.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The worker could not be terminated; the parent still reports the failure.
        }
    }
}
