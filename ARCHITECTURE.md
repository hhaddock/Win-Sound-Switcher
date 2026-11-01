# Sound Switcher Architecture

## Goal

Provide one fast action to change the Windows default render device among any
number of user-configured outputs.

## Runtime shape

```text
Global hotkey / tray / overlay
            |
      TrayApplication
            |
  DeviceSwitchService
            |
IAudioDeviceService
            |
  svcl.exe (one process per operation)
            |
 Windows default audio policy
```

There is one application process. There is no audio worker, NAudio dependency,
custom policy COM implementation, MSIX dependency, or background polling loop.

## Audio operations

The app bundles NirSoft SoundVolumeCommandLine as `svcl.exe`. It uses:

- `/scomma ""` to enumerate active render devices;
- `/SetDefault <command-line-id> all` to set the default device for all roles.

The adapter translates the utility's command-line device ID to the endpoint ID
stored in the app settings. The utility is invoked with `ProcessStartInfo` and
an argument list, never a shell command string.

Every operation is serialized through one semaphore. Each external process has
a five-second timeout and is killed if it does not exit. Device enumeration is
cached for three seconds to avoid repeated queries while opening the UI.

## Configuration and UX

Settings store endpoint IDs, friendly labels, order, enabled state, and the
global hotkey in the user's application-data directory. The tray and overlay
use the same switch service, so hotkey, menu, and visual selection cannot issue
overlapping audio commands.

Disconnected devices remain in configuration but are skipped during cycling.
The app reports an error in the tray only when a requested operation fails; it
does not emit success notifications for normal switches.

## Distribution

The supported release is a self-contained `win-x64` portable folder containing
`SoundSwitcher.exe`, `svcl.exe`, `svcl.chm`, and `svcl-readme.txt`. The bundled
utility is distributed unmodified with its license/readme.

## Deliberate non-goals

- Per-application audio routing
- Input-device or microphone management
- Volume, mute, EQ, or mixer controls
- Cross-platform support
