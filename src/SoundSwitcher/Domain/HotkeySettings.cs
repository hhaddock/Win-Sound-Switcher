using System.Text.Json.Serialization;

namespace SoundSwitcher.Domain;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// A global hotkey represented without a dependency on WPF or Win32 types.
/// </summary>
public sealed record HotkeySettings
{
    public HotkeySettings(int virtualKey, HotkeyModifiers modifiers)
    {
        if (virtualKey <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualKey), "A hotkey must have a virtual-key code.");
        }

        if (modifiers == HotkeyModifiers.None)
        {
            throw new ArgumentException("A global hotkey must have at least one modifier.", nameof(modifiers));
        }

        VirtualKey = virtualKey;
        Modifiers = modifiers;
    }

    /// <summary>
    /// The Windows virtual-key code. The default S key is 0x53.
    /// </summary>
    [JsonPropertyName("virtualKey")]
    public int VirtualKey { get; }

    [JsonPropertyName("modifiers")]
    public HotkeyModifiers Modifiers { get; }

    public static HotkeySettings Default { get; } = new(0x53, HotkeyModifiers.Control | HotkeyModifiers.Alt);
}
