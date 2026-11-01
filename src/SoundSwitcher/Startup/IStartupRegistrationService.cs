namespace SoundSwitcher.Startup;

public interface IStartupRegistrationService
{
    void SetEnabled(bool enabled);

    bool IsEnabled();
}
