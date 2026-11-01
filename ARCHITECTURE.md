# Sound Switcher Architecture

**Status:** Agreed for implementation  
**Target platform:** Windows 11  
**Application type:** Per-user desktop utility

## 1. Purpose

Sound Switcher provides a fast way to change the Windows default audio output among any number of user-selected devices, such as headphones, speakers, a monitor, or a USB audio interface.

The application should remain out of the way during normal use and be accessible through a global hotkey and a system-tray menu.

## 2. Goals

- Switch among any number of configured output devices with one action.
- Make the current output device obvious.
- Provide immediate visual feedback while switching.
- Support keyboard-driven switching as well as mouse interaction.
- Work as a background tray application without requiring administrator privileges.
- Remember devices by their Windows endpoint IDs rather than display names.
- Handle disconnected or renamed devices gracefully.
- Start with Windows optionally.
- Produce a small, self-contained Windows executable for distribution.

## 3. Non-goals for the first version

- Managing input devices or microphones.
- Controlling application-specific audio routing.
- Changing volume, mute state, equalizer settings, or other sound settings.
- Supporting multiple operating systems.
- Replacing the full Windows sound settings experience.
- Automatically guessing which device is a pair of headphones or speakers.

## 4. Proposed technology stack

- **Language:** C#
- **Framework:** Modern .NET with Windows desktop support
- **Desktop UI:** WPF
- **Tray integration:** Windows tray icon through the Windows Forms tray API, hosted by the WPF application
- **Audio control:** Windows Core Audio APIs behind an application-owned adapter, implemented with `NAudio.Wasapi`
- **Persistence:** JSON settings file in the current user's application-data directory
- **Hotkey:** Windows global hotkey registration API
- **Distribution:** Self-contained Windows build containing the tray executable and an isolated audio-switch worker

The application isolates Core Audio access behind `IAudioDeviceService`. The current implementation uses `NAudio.Wasapi` only inside the audio infrastructure adapter; the rest of the application does not depend directly on NAudio types. This keeps the package replaceable if it becomes unsuitable later.

## 5. User experience

### First launch

1. The application enumerates active audio render devices.
2. A small setup window lists available output devices.
3. The user selects and orders any number of devices to include in the switcher.
4. The user can assign friendly labels such as `Headphones` and `Speakers`.
5. The application saves the devices' endpoint IDs.
6. The application moves to the system tray.

If no output devices are available, setup remains incomplete and explains what is missing. A single configured device is valid, but switching is disabled until another device is configured.

### Normal use

- Press the configured global hotkey to advance to the next available configured device.
- A compact switcher overlay appears near the center of the primary display, highlights the selected device, and briefly shows the full configured list.
- Press the hotkey repeatedly to cycle through the list. The selected device becomes active immediately, so the common case remains one keystroke.
- Use number keys or arrow keys while the overlay is visible to select a specific device.
- Click the tray icon to open the switcher overlay.
- Right-click the tray icon to open a menu containing:
  - Open switcher
  - Switch to each configured device
  - Reconfigure devices
  - Start with Windows
  - Exit

The hotkey changes the device immediately. The overlay also supports deliberate direct selection: choosing a device with the mouse or keyboard applies that device, while Escape closes the overlay without making an additional selection.

After a successful switch, the overlay, tray icon tooltip, and a short notification identify the active device.

### Missing devices

If a configured device is disconnected, it remains in the configuration but is shown as unavailable. The user can still switch to the connected device or reopen configuration to select a replacement.

The application must never silently substitute an unrelated device.

## 6. High-level architecture

```text
Windows tray / setup UI / switcher overlay
            |
       Application layer
            |
     Audio device interface
            |
   Core Audio infrastructure
            |
        Windows Audio
```

### Presentation layer

Responsible for the setup window, tray icon, context menu, switcher overlay, notifications, and displaying errors to the user. It should not contain Core Audio or settings-file logic.

The switcher overlay should be a small, borderless, keyboard-friendly WPF window. It should dismiss automatically after a short timeout and close with Escape. The visual design should make the current/default device, the selected device, and unavailable devices distinguishable at a glance.

### Application layer

Coordinates the use cases:

- Advance to the next available configured device.
- Select a specific configured device.
- Open and control the switcher overlay.
- Reconfigure devices.
- Load and save preferences.
- Register and handle the global hotkey.

This layer owns the switching rules and determines whether a device is available before requesting a switch.

### Audio infrastructure

Provides an adapter around Windows Core Audio. Its responsibilities are:

- Enumerate output/render devices.
- Resolve a configured output endpoint by stable ID and report whether it is currently available.
- Read the current default output device.
- Set the Windows default output device.
- Report device availability and stable endpoint IDs.

The current implementation reads the Multimedia default endpoint. Setting a normal output updates the Console and Multimedia roles; the Communications role remains separate until the product explicitly needs communications-device control.

The rest of the application should depend on an interface such as `IAudioDeviceService`, not on COM types or a third-party package.

### Audio switch fault boundary

Changing the Windows default endpoint uses an undocumented Windows policy COM
interface. A failure in that native boundary can terminate the hosting process
with an unmanaged stack overflow that managed exception handling cannot recover
from. Therefore the release application uses two processes:

```text
Tray UI / application layer
            |
   IAudioDeviceService adapter
            |
 SoundSwitcher.AudioWorker.exe
            |
 Windows policy COM + NAudio
```

Enumeration, default-device reads, endpoint reads, and mutation all run in a
short-lived worker process. A worker exception, non-zero exit, timeout, or
native crash is converted into an audio error while the tray process remains
available. The worker must be packaged beside `SoundSwitcher.exe`.

### Persistence infrastructure

Stores user preferences in a versioned JSON file under the current user's application-data directory. The persisted configuration should include:

- An ordered list of configured endpoint IDs.
- User-facing labels for each configured device.
- Whether each configured device is enabled in the switcher.
- The hotkey configuration.
- Whether startup with Windows is enabled.
- A settings schema version.

The file should be tolerant of new fields being added in future versions.

## 7. Core switching behavior

The next-device operation should follow this sequence:

1. Load the current configuration.
2. Query the current Windows default output device.
3. Find the next enabled configured endpoint after the current device, wrapping at the end of the ordered list.
4. Skip configured endpoints that are currently unavailable.
5. Set the target as the Windows default output device.
6. Refresh the overlay and tray state and notify the user.

If the current default device is not configured, switching should select the first enabled configured device that is available. If no configured device is available, the operation should fail with an actionable message.

The operation should be serialized so two rapid hotkey presses cannot race each other or leave the tray state stale.

The overlay should use the same application-layer commands as the tray menu and hotkey. It must not contain separate device-switching logic.

## 8. Application lifecycle

1. Start the process.
2. Acquire a per-user single-instance mutex.
3. Load and validate settings.
4. Initialize the audio service.
5. Register the global hotkey.
6. Create the tray icon and menu.
7. Create the switcher overlay on demand.
8. Show setup if configuration is missing or invalid.
9. Run until the user selects Exit.
10. Unregister the hotkey and dispose the tray/audio resources. Worker processes
    are short-lived and are terminated on timeout or cancellation.

Launching a second instance should focus or reopen the existing setup window if possible, then exit immediately.

## 9. Error handling

Errors should be translated into user-facing messages rather than exposing COM or exception details.

Important cases include:

- No output devices are detected.
- A configured device has been disconnected.
- Windows rejects the default-device change.
- The hotkey is already registered by another application.
- Settings are unreadable or malformed.
- The application cannot create its data directory.

Settings corruption should be recoverable: preserve the invalid file with a backup suffix when practical, recreate defaults, and send the user through setup again.

## 10. Security and permissions

- Run as a normal user process.
- Do not request administrator elevation.
- Do not install a driver or Windows service.
- Store only local preferences; no network access is required.
- Avoid collecting or transmitting audio-device information.

## 11. Testing strategy

### Unit tests

- Ordered cycling through any number of configured devices.
- Direct selection of a configured device.
- Handling of unavailable devices.
- Configuration validation and migration.
- Behavior when the current default is not one of the configured devices.

### Integration tests

- Enumerating real Windows output devices.
- Setting the default output device.
- Registering and releasing the hotkey.
- Loading and saving settings in the expected application-data location.

### Manual acceptance test

On a Windows 11 machine with several output devices connected:

1. Launch the application.
2. Complete setup.
3. Press the hotkey repeatedly and confirm the overlay cycles through the ordered list.
4. Confirm Windows changes the default output each time.
5. Select a specific device from the overlay with the mouse, number keys, and arrow keys.
6. Disconnect one device and confirm the application reports it as unavailable and skips it during cycling.
7. Reconnect it and confirm it becomes selectable again.
8. Enable startup and confirm the tray application launches after sign-in.

## 12. Initial implementation milestones

1. Create the .NET solution and project structure.
2. Implement and test Core Audio device enumeration.
3. Implement changing the default output device.
4. Add the domain/application switching logic.
5. Add settings persistence.
6. Add the tray icon and context menu.
7. Add first-run setup.
8. Add global hotkey support.
9. Add the switcher overlay and keyboard navigation.
10. Add startup registration and packaging.

Each milestone should leave the project buildable and runnable.

## 13. Decisions still open

- The specific Core Audio NuGet wrapper, if one is used.
- Whether number keys should select devices directly when more than nine devices are configured.

The following product decisions are agreed:

- `Ctrl+Alt+S` is the initial hotkey.
- Each hotkey press immediately advances to the next available configured device.
- The overlay provides visual feedback and direct mouse/keyboard selection.
- Devices are manually ordered and cycling skips disconnected or disabled devices.
- The first release is a portable self-contained executable.
- Launch with Windows is optional and disabled by default.
- Integration testing will use the current Windows machine, which has several audio outputs available.

The current audio implementation uses the stable `NAudio.Wasapi` 3.3.0 package behind the adapter boundary.

