using System.Diagnostics;
using System.Text;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Audio;

/// <summary>
/// Uses the established SoundVolumeCommandLine utility for all Windows audio
/// access. The tray process never loads NAudio or calls the undocumented policy
/// COM interface directly.
/// </summary>
public sealed class SoundVolumeAudioDeviceService : IAudioDeviceService, IDisposable
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(5);
    private readonly string _executablePath;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private CachedDeviceList? _cache;

    public SoundVolumeAudioDeviceService(string executablePath)
    {
        _executablePath = string.IsNullOrWhiteSpace(executablePath)
            ? throw new ArgumentException("The sound command path is required.", nameof(executablePath))
            : executablePath;
    }

    public async Task<IReadOnlyList<AudioDevice>> GetOutputDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            return (await GetDevicesCoreAsync(cancellationToken))
                .Select(device => new AudioDevice(device.EndpointId, device.DisplayName, true))
                .ToArray();
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<AudioDevice?> GetOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var device = (await GetDevicesCoreAsync(cancellationToken))
                .FirstOrDefault(item => string.Equals(
                    item.EndpointId,
                    endpointId,
                    StringComparison.OrdinalIgnoreCase));

            return device is null
                ? null
                : new AudioDevice(device.EndpointId, device.DisplayName, true);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<AudioDevice?> GetDefaultOutputDeviceAsync(
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var device = (await GetDevicesCoreAsync(cancellationToken))
                .FirstOrDefault(item => item.IsDefault);

            return device is null
                ? null
                : new AudioDevice(device.EndpointId, device.DisplayName, true);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task SetDefaultOutputDeviceAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var device = (await GetDevicesCoreAsync(cancellationToken))
                .FirstOrDefault(item => string.Equals(
                    item.EndpointId,
                    endpointId,
                    StringComparison.OrdinalIgnoreCase));

            if (device is null)
            {
                throw new InvalidOperationException("The requested audio device is not active.");
            }

            await RunCommandAsync(
                ["/SetDefault", device.CommandLineId, "all"],
                cancellationToken);

            _cache = null;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public void Dispose() => _operationGate.Dispose();

    private async Task<IReadOnlyList<SoundVolumeDevice>> GetDevicesCoreAsync(
        CancellationToken cancellationToken)
    {
        if (_cache is not null && _cache.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return _cache.Devices;
        }

        var csv = await RunCommandAsync(["/scomma", string.Empty], cancellationToken);
        var devices = ParseDevices(csv);
        _cache = new CachedDeviceList(devices, DateTimeOffset.UtcNow.AddSeconds(3));
        return devices;
    }

    private async Task<string> RunCommandAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("SoundVolumeCommandLine could not be started.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).WaitAsync(CommandTimeout, cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(error)
                        ? $"SoundVolumeCommandLine exited with code {process.ExitCode}."
                        : error.Trim());
            }

            return output;
        }
        catch
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
                // The command exited between the check and Kill.
            }

            throw;
        }
    }

    private static IReadOnlyList<SoundVolumeDevice> ParseDevices(string csv)
    {
        var rows = CsvParser.Parse(csv);
        if (rows.Count == 0)
        {
            return [];
        }

        var headers = rows[0]
            .Select((value, index) => new { Name = value.Trim(), Index = index })
            .Where(value => value.Name.Length > 0)
            .ToDictionary(value => value.Name, value => value.Index, StringComparer.OrdinalIgnoreCase);

        var devices = new List<SoundVolumeDevice>();
        foreach (var row in rows.Skip(1))
        {
            var type = Get(row, headers, "Type");
            var direction = Get(row, headers, "Direction");
            var state = Get(row, headers, "Device State");
            var endpointId = Get(row, headers, "Item ID");
            var commandLineId = Get(row, headers, "Command-Line Friendly ID");
            var displayName = Get(row, headers, "Name");

            if (!string.Equals(type, "Device", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(direction, "Render", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(state, "Active", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(endpointId)
                || string.IsNullOrWhiteSpace(commandLineId)
                || string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            devices.Add(new SoundVolumeDevice(
                endpointId,
                displayName,
                commandLineId,
                IsDefault(row, headers)));
        }

        return devices;
    }

    private static bool IsDefault(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> headers) =>
        string.Equals(Get(row, headers, "Default"), "Render", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Get(row, headers, "Default Multimedia"), "Render", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Get(row, headers, "Default Communications"), "Render", StringComparison.OrdinalIgnoreCase);

    private static string Get(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> headers,
        string name) =>
        headers.TryGetValue(name, out var index) && index < row.Count
            ? row[index].Trim()
            : string.Empty;

    private sealed record SoundVolumeDevice(
        string EndpointId,
        string DisplayName,
        string CommandLineId,
        bool IsDefault);

    private sealed record CachedDeviceList(
        IReadOnlyList<SoundVolumeDevice> Devices,
        DateTimeOffset ExpiresAt);

    private static class CsvParser
    {
        public static IReadOnlyList<IReadOnlyList<string>> Parse(string input)
        {
            var rows = new List<IReadOnlyList<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;

            for (var index = 0; index < input.Length; index++)
            {
                var character = input[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < input.Length && input[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }

                    continue;
                }

                if (!quoted && character == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (!quoted && (character == '\r' || character == '\n'))
                {
                    if (character == '\r' && index + 1 < input.Length && input[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    field.Clear();
                    if (row.Any(value => value.Length > 0))
                    {
                        rows.Add(row);
                    }

                    row = new List<string>();
                    continue;
                }

                field.Append(character);
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
