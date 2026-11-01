using SoundSwitcher.Domain;

namespace SoundSwitcher.Persistence;

public interface ISettingsStore
{
    Task<SoundSwitcherSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        SoundSwitcherSettings settings,
        CancellationToken cancellationToken = default);
}
