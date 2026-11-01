using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;
using SoundSwitcher.Audio;
using SoundSwitcher.Configuration;
using SoundSwitcher.Domain;
using SoundSwitcher.Persistence;

namespace SoundSwitcher;

public partial class SetupWindow : Window
{
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly ISettingsStore _settingsStore;
    private readonly DeviceConfigurationService _configurationService;
    private SoundSwitcherSettings _settings;
    private HotkeySettings _hotkey;

    public SetupWindow()
        : this(
            new NAudioDeviceService(),
            new SettingsStore(),
            new SoundSwitcherSettings([]))
    {
    }

    public SetupWindow(
        IAudioDeviceService audioDeviceService,
        ISettingsStore settingsStore,
        SoundSwitcherSettings settings)
    {
        _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _configurationService = new DeviceConfigurationService();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _hotkey = _settings.Hotkey;

        InitializeComponent();
        HotkeyBox.Text = FormatHotkey(_hotkey);
        LaunchAtStartupCheckBox.IsChecked = _settings.LaunchAtStartup;
        DataContext = this;
        Loaded += SetupWindow_Loaded;
    }

    public ObservableCollection<ConfigurationDeviceItem> Items { get; } = [];

    public ConfigurationDeviceItem? SelectedItem
    {
        get => DeviceList.SelectedItem as ConfigurationDeviceItem;
        set => DeviceList.SelectedItem = value;
    }

    private async void SetupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= SetupWindow_Loaded;
        await RefreshDevicesAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        try
        {
            var discoveredDevices = await _audioDeviceService.GetOutputDevicesAsync();
            var mergedItems = _configurationService.Merge(
                _settings.ConfiguredDevices,
                discoveredDevices);

            Items.Clear();
            foreach (var item in mergedItems)
            {
                Items.Add(item);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            MessageBox.Show(
                this,
                $"Audio devices could not be loaded.{Environment.NewLine}{exception.Message}",
                "Sound Switcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedItem(-1);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedItem(1);
    }

    private void MoveSelectedItem(int offset)
    {
        if (SelectedItem is null)
        {
            return;
        }

        var currentIndex = Items.IndexOf(SelectedItem);
        var targetIndex = currentIndex + offset;

        if (targetIndex < 0 || targetIndex >= Items.Count)
        {
            return;
        }

        Items.Move(currentIndex, targetIndex);
        SelectedItem = Items[targetIndex];
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is null)
        {
            return;
        }

        Items.Remove(SelectedItem);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newSettings = _configurationService.CreateSettings(
                Items,
                _hotkey,
                LaunchAtStartupCheckBox.IsChecked == true,
                _settings.SchemaVersion);

            await _settingsStore.SaveAsync(newSettings);
            _settings = newSettings;
            DialogResult = true;
        }
        catch (ArgumentException exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Sound Switcher",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (IOException exception)
        {
            MessageBox.Show(
                this,
                $"Settings could not be saved.{Environment.NewLine}{exception.Message}",
                "Sound Switcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftAlt or Key.RightAlt
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }

        var modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        if (modifiers == HotkeyModifiers.None)
        {
            e.Handled = true;
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        _hotkey = new HotkeySettings(virtualKey, modifiers);
        HotkeyBox.Text = FormatHotkey(_hotkey);
        e.Handled = true;
    }

    private static string FormatHotkey(HotkeySettings hotkey)
    {
        var parts = new List<string>();
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(hotkey.VirtualKey));
        return string.Join("+", parts);
    }

    private static string FormatKey(int virtualKey)
    {
        var key = KeyInterop.KeyFromVirtualKey(virtualKey);

        return key switch
        {
            Key.D0 or Key.D1 or Key.D2 or Key.D3 or Key.D4
                or Key.D5 or Key.D6 or Key.D7 or Key.D8 or Key.D9
                => ((int)key - (int)Key.D0).ToString(),
            Key.NumPad0 or Key.NumPad1 or Key.NumPad2 or Key.NumPad3 or Key.NumPad4
                or Key.NumPad5 or Key.NumPad6 or Key.NumPad7 or Key.NumPad8 or Key.NumPad9
                => $"Num {(int)key - (int)Key.NumPad0}",
            Key.Back => "Backspace",
            Key.Return => "Enter",
            Key.Prior => "PageUp",
            Key.Next => "PageDown",
            Key.Capital => "CapsLock",
            Key.OemPlus => "Plus",
            Key.OemMinus => "Minus",
            Key.OemComma => "Comma",
            Key.OemPeriod => "Period",
            Key.OemQuestion => "Question",
            Key.OemSemicolon => "Semicolon",
            Key.OemQuotes => "Quote",
            Key.OemOpenBrackets => "LeftBracket",
            Key.OemCloseBrackets => "RightBracket",
            Key.OemBackslash => "Backslash",
            Key.None => $"Key (0x{virtualKey:X2})",
            _ => key.ToString()
        };
    }
}
