using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using SoundSwitcher.Audio;
using SoundSwitcher.Domain;
using SoundSwitcher.Persistence;
using SoundSwitcher.Overlay;
using SoundSwitcher.Startup;
using SoundSwitcher.Switching;

namespace SoundSwitcher.Presentation;

/// <summary>
/// Owns the Windows notification-area icon and its application commands.
/// </summary>
public sealed class TrayApplication : IDisposable
{
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly ISettingsStore _settingsStore;
    private readonly DeviceSwitchService _deviceSwitchService;
    private readonly IStartupRegistrationService _startupRegistrationService;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _trayIcon;
    private SoundSwitcherSettings _settings;
    private bool _isDisposed;

    public TrayApplication(
        IAudioDeviceService audioDeviceService,
        ISettingsStore settingsStore,
        DeviceSwitchService deviceSwitchService,
        IStartupRegistrationService startupRegistrationService,
        SoundSwitcherSettings settings)
    {
        _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _deviceSwitchService = deviceSwitchService ?? throw new ArgumentNullException(nameof(deviceSwitchService));
        _startupRegistrationService = startupRegistrationService
            ?? throw new ArgumentNullException(nameof(startupRegistrationService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _trayIcon = LoadApplicationIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Visible = true
        };
    }

    public event EventHandler? SwitcherRequested;

    public event EventHandler? ConfigurationChanged;

    public void Start()
    {
        ThrowIfDisposed();
        RebuildContextMenu();
        _ = RefreshActiveDeviceAsync();
    }

    public void UpdateSettings(SoundSwitcherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfDisposed();

        _settings = settings;
        _startupRegistrationService.SetEnabled(settings.LaunchAtStartup);
        RebuildContextMenu();
        _ = RefreshActiveDeviceAsync();
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ShowConfiguration()
    {
        ThrowIfDisposed();

        var setupWindow = new SetupWindow(
            _audioDeviceService,
            _settingsStore,
            _settings);

        if (setupWindow.ShowDialog() == true)
        {
            _ = ReloadSettingsAsync();
        }
    }

    public async Task SwitchToNextAsync()
    {
        ThrowIfDisposed();

        try
        {
            var currentDevice = await _audioDeviceService.GetDefaultOutputDeviceAsync();
            var availableDevices = await _audioDeviceService.GetOutputDevicesAsync();
            var selector = new DeviceSelectionService();
            var nextDevice = selector.SelectNext(
                _settings.ConfiguredDevices,
                availableDevices,
                currentDevice?.EndpointId);

            if (nextDevice is null)
            {
                _notifyIcon.ShowBalloonTip(
                    2500,
                    "Sound Switcher",
                    "No other configured audio device is currently available.",
                    ToolTipIcon.Warning);
                return;
            }

            await SwitchToAsync(nextDevice.EndpointId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                "Sound Switcher",
                $"Audio output could not be changed: {exception.Message}",
                ToolTipIcon.Error);
        }
    }

    public async Task ShowSwitcherAsync()
    {
        ThrowIfDisposed();

        try
        {
            var availableDevices = await _audioDeviceService.GetOutputDevicesAsync();
            var currentDevice = await _audioDeviceService.GetDefaultOutputDeviceAsync();
            var switcherWindow = new SwitcherWindow(
                _settings.ConfiguredDevices,
                availableDevices,
                currentDevice?.EndpointId);

            if (switcherWindow.ShowDialog() == true
                && switcherWindow.SelectedEndpointId is not null)
            {
                await SwitchToAsync(switcherWindow.SelectedEndpointId);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                "Sound Switcher",
                $"The switcher could not open: {exception.Message}",
                ToolTipIcon.Error);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        if (!ReferenceEquals(_trayIcon, SystemIcons.Application))
        {
            _trayIcon.Dispose();
        }
    }

    private void RebuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var openSwitcher = new ToolStripMenuItem("Open switcher");
        openSwitcher.Click += (_, _) => SwitcherRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(openSwitcher);

        var directSelection = new ToolStripMenuItem("Switch to");
        foreach (var device in _settings.ConfiguredDevices)
        {
            var item = new ToolStripMenuItem(device.Label)
            {
                Tag = device.EndpointId,
                Enabled = device.IsEnabled
            };
            item.Click += async (_, _) => await SwitchToAsync((string)item.Tag!);
            directSelection.DropDownItems.Add(item);
        }

        directSelection.Enabled = directSelection.DropDownItems.Count > 0;
        menu.Items.Add(directSelection);
        menu.Items.Add(new ToolStripSeparator());

        var configure = new ToolStripMenuItem("Configure devices...");
        configure.Click += (_, _) => ShowConfiguration();
        menu.Items.Add(configure);

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => System.Windows.Application.Current.Shutdown();
        menu.Items.Add(exit);

        var oldMenu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = menu;
        oldMenu?.Dispose();
    }

    private async Task SwitchToAsync(string endpointId)
    {
        try
        {
            var activeDevice = await _deviceSwitchService.SelectDeviceAsync(
                _settings.ConfiguredDevices,
                endpointId);

            if (activeDevice is null)
            {
                _notifyIcon.ShowBalloonTip(
                    2500,
                    "Sound Switcher",
                    "That device is unavailable or not enabled. Use Configure devices to replace it.",
                    ToolTipIcon.Warning);
                return;
            }

            SetTooltip(activeDevice.DisplayName);
            _notifyIcon.ShowBalloonTip(
                1800,
                "Sound Switcher",
                $"Switched to {activeDevice.DisplayName}.",
                ToolTipIcon.Info);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                "Sound Switcher",
                $"Audio output could not be changed: {exception.Message}",
                ToolTipIcon.Error);
        }
    }

    private async Task ReloadSettingsAsync()
    {
        try
        {
            UpdateSettings(await _settingsStore.LoadAsync());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                "Sound Switcher",
                $"Settings could not be reloaded: {exception.Message}",
                ToolTipIcon.Error);
        }
    }

    private async Task RefreshActiveDeviceAsync()
    {
        try
        {
            var activeDevice = await _audioDeviceService.GetDefaultOutputDeviceAsync();
            SetTooltip(activeDevice?.DisplayName);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetTooltip(null);
        }
    }

    private void SetTooltip(string? activeDeviceName)
    {
        var text = string.IsNullOrWhiteSpace(activeDeviceName)
            ? "Sound Switcher"
            : $"Sound Switcher - {activeDeviceName}";

        _notifyIcon.Text = text.Length <= 63 ? text : text[..63];
    }

    private static Icon LoadApplicationIcon()
    {
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var applicationIcon = Icon.ExtractAssociatedIcon(executablePath);
            if (applicationIcon is not null)
            {
                return applicationIcon;
            }
        }

        return SystemIcons.Application;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
