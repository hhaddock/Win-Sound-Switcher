using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using SoundSwitcher.Audio;
using SoundSwitcher.Infrastructure;
using SoundSwitcher.Hotkeys;
using SoundSwitcher.Persistence;
using SoundSwitcher.Presentation;
using SoundSwitcher.Startup;
using SoundSwitcher.Switching;

namespace SoundSwitcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private SingleInstanceGuard? _singleInstanceGuard;
    private TrayApplication? _trayApplication;
    private GlobalHotkeyService? _hotkeyService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _singleInstanceGuard = new SingleInstanceGuard();

        if (!_singleInstanceGuard.TryAcquire())
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        try
        {
            var audioDeviceService = new NAudioDeviceService();
            var audioWorkerPath = Path.Combine(
                AppContext.BaseDirectory,
                "SoundSwitcher.AudioWorker.exe");
            IAudioDeviceService safeAudioDeviceService = File.Exists(audioWorkerPath)
                ? new IsolatedAudioDeviceService(audioWorkerPath)
                : audioDeviceService;
            var settingsStore = new SettingsStore();
            var settings = await settingsStore.LoadAsync();
            var switchService = new DeviceSwitchService(safeAudioDeviceService);
            var startupRegistrationService = new WindowsStartupRegistrationService();

            _trayApplication = new TrayApplication(
                safeAudioDeviceService,
                settingsStore,
                switchService,
                startupRegistrationService,
                settings);
            _trayApplication.SwitcherRequested += (_, _) => _ = _trayApplication.ShowSwitcherAsync();
            _trayApplication.Start();
            _hotkeyService = new GlobalHotkeyService(settings.Hotkey);
            _hotkeyService.Pressed += async (_, _) =>
            {
                await _trayApplication.SwitchToNextAsync();
                await _trayApplication.ShowSwitcherAsync();
            };
            _hotkeyService.Start();

            if (settings.ConfiguredDevices.Count == 0)
            {
                _trayApplication.ShowConfiguration();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            MessageBox.Show(
                $"Sound Switcher could not start.{Environment.NewLine}{exception.Message}",
                "Sound Switcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayApplication?.Dispose();
        _hotkeyService?.Dispose();
        _singleInstanceGuard?.Dispose();
        base.OnExit(e);
    }
}
