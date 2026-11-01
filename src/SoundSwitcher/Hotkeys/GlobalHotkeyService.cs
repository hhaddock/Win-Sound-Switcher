using System.Runtime.InteropServices;
using System.Windows.Interop;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Hotkeys;

/// <summary>
/// Registers one global hotkey against a hidden native window.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWindows = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private const int HotkeyId = 0x534F;
    private const int WsPopup = unchecked((int)0x80000000);
    private const int WsExToolWindow = 0x00000080;

    private readonly HotkeySettings _settings;
    private HwndSource? _source;
    private bool _registered;

    public GlobalHotkeyService(HotkeySettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public event EventHandler? Pressed;

    public void Start()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("SoundSwitcher.GlobalHotkey")
        {
            Width = 1,
            Height = 1,
            WindowStyle = WsPopup,
            ExtendedWindowStyle = WsExToolWindow
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WindowProcedure);

        if (!RegisterHotKey(
                _source.Handle,
                HotkeyId,
                ToNativeModifiers(_settings.Modifiers),
                (uint)_settings.VirtualKey))
        {
            _source.RemoveHook(WindowProcedure);
            _source.Dispose();
            _source = null;
            throw new InvalidOperationException(
                "The configured global hotkey is already in use by another application.");
        }

        _registered = true;
    }

    public void Dispose()
    {
        if (_source is null)
        {
            return;
        }

        if (_registered)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
            _registered = false;
        }

        _source.RemoveHook(WindowProcedure);
        _source.Dispose();
        _source = null;
    }

    private static uint ToNativeModifiers(HotkeyModifiers modifiers) =>
        (uint)modifiers | ModNoRepeat;

    private IntPtr WindowProcedure(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == WmHotKey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
