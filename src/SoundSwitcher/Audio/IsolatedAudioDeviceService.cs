using System.Diagnostics;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Audio;

/// <summary>
/// Keeps the tray process alive if the Windows audio-policy COM boundary fails.
/// Discovery remains in-process; only the risky default-endpoint mutation runs
/// in a short-lived worker process.
/// </summary>
public sealed class IsolatedAudioDeviceService : IAudioDeviceService
{
    private static readonly TimeSpan SwitchTimeout = TimeSpan.FromSeconds(10);

    private readonly IAudioDeviceService _discoveryService;
    private readonly string _workerPath;

    public IsolatedAudioDeviceService(
        IAudioDeviceService discoveryService,
        string workerPath)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _workerPath = string.IsNullOrWhiteSpace(workerPath)
            ? throw new ArgumentException("An audio worker path is required.", nameof(workerPath))
            : workerPath;
    }

    public Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(
        CancellationToken cancellationToken = default) =>
        _discoveryService.GetOutputDevicesAsync(cancellationToken);

    public Task<AudioDevice?> GetOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default) =>
        _discoveryService.GetOutputDeviceAsync(endpointId, cancellationToken);

    public Task<AudioDevice?> GetDefaultOutputDeviceAsync(
        CancellationToken cancellationToken = default) =>
        _discoveryService.GetDefaultOutputDeviceAsync(cancellationToken);

    public async Task SetDefaultOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = _workerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add(endpointId);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The audio switch worker could not be started.");

        try
        {
            await process.WaitForExitAsync(cancellationToken).WaitAsync(SwitchTimeout, cancellationToken);
        }
        catch
        {
            TryTerminate(process);
            throw;
        }

        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(error)
                ? $"The audio switch worker exited with code {process.ExitCode}."
                : error.Trim();

            throw new InvalidOperationException(detail);
        }
    }

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
