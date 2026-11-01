using System.IO;
using System.Text.Json;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Persistence;

/// <summary>
/// Persists user settings as JSON in the current user's application-data folder.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;

    public SettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultFilePath();
    }

    public string FilePath => _filePath;

    public async Task<SoundSwitcherSettings> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            return CreateDefaultSettings();
        }

        try
        {
            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var settings = await JsonSerializer.DeserializeAsync<SoundSwitcherSettings>(
                stream,
                SerializerOptions,
                cancellationToken);

            return settings ?? CreateDefaultSettings();
        }
        catch (JsonException)
        {
            BackupMalformedFile();
            return CreateDefaultSettings();
        }
        catch (NotSupportedException)
        {
            BackupMalformedFile();
            return CreateDefaultSettings();
        }
        catch (ArgumentException)
        {
            BackupMalformedFile();
            return CreateDefaultSettings();
        }
    }

    public async Task SaveAsync(
        SoundSwitcherSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The settings file path must include a directory.");
        }

        Directory.CreateDirectory(directory);

        var temporaryPath = string.Concat(
            _filePath,
            ".",
            Guid.NewGuid().ToString("N"),
            ".tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    SerializerOptions,
                    cancellationToken);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string GetDefaultFilePath()
    {
        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        return Path.Combine(applicationData, "SoundSwitcher", "settings.json");
    }

    private static SoundSwitcherSettings CreateDefaultSettings() =>
        new([]);

    private void BackupMalformedFile()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var backupPath = string.Concat(
            _filePath,
            ".invalid-",
            DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));

        try
        {
            File.Move(_filePath, backupPath);
        }
        catch (IOException)
        {
            // Recovery should still return usable defaults if the backup cannot
            // be created because another process has touched the file.
        }
        catch (UnauthorizedAccessException)
        {
            // The caller still receives defaults; the original file is retained.
        }
    }
}
