# Sound Switcher Kanban Plan

**Project status:** Implementation-ready  
**Board status:** FND-01, FND-02, FND-03, AUD-01, AUD-02, AUD-03, AUD-04, CFG-01, CFG-02, SW-01, SW-02, SW-03, CFG-03, UI-01, UI-02, UI-03, OVR-01, OVR-02, OVR-03, REL-01, REL-02, and REL-04 are Done; REL-03 remains in Review  
**Source architecture:** [ARCHITECTURE.md](ARCHITECTURE.md)

## Current board snapshot

### Done

- FND-01 — Create the solution and project structure
- FND-02 — Define application contracts and models
- AUD-01 — Enumerate output devices
- AUD-02 — Read the current default output
- AUD-03 — Set the default output
- AUD-04 — Handle device availability
- FND-03 — Add single-instance application behavior
- CFG-01 — Define the settings schema
- SW-01 — Implement ordered device cycling
- SW-02 — Implement direct device selection
- SW-03 — Serialize switching operations
- CFG-02 — Implement settings persistence
- CFG-03 — Build first-run configuration
- UI-01 — Add the system-tray application
- UI-02 — Add the global hotkey
- UI-03 — Add startup-with-Windows support
- OVR-01 — Create the visual switcher overlay
- OVR-02 — Add overlay selection controls
- OVR-03 — Add switch feedback and error presentation
- REL-01 — Add unit and integration tests
- REL-02 — Package the application
- REL-04 — Keep the audio path minimal and deterministic

### Ready / next candidates

- REL-03 — Perform end-to-end acceptance testing (review: physical disconnect/reconnect and sign-in verification pending)

### Backlog

All other cards remain in Backlog until their dependencies and scope are selected.

## Agreed product decisions

- The application supports any number of pre-configured output devices.
- Devices have a user-defined order; cycling skips disconnected and disabled devices.
- `Ctrl+Alt+S` immediately switches to the next available device.
- A compact overlay appears as visual feedback and supports direct mouse/keyboard selection.
- The overlay is Windows theme-aware, centered on the primary display, and dismisses after a short timeout.
- The first release is a portable self-contained executable.
- Launch with Windows is optional and disabled by default.
- Integration testing will use the current Windows machine with several output devices.

## Board columns

### Backlog

Ideas and implementation cards that have not yet been selected.

### Ready

Cards with clear acceptance criteria and no unresolved dependency.

### In Progress

Cards currently being implemented. Keep this column limited to active work.

### Review / Test

Cards awaiting code review, manual verification, or integration testing.

### Done

Cards whose acceptance criteria are met and whose changes are integrated.

### Blocked

Cards that cannot proceed because of a concrete external dependency or design decision. The blocking reason must be written on the card.

## Working agreements

- Move a card only when its current state is clear.
- Keep cards small enough to complete without hiding unrelated work inside them.
- Do not start a card until its dependencies are complete or explicitly stubbed.
- A card is not Done merely because it compiles; its acceptance criteria must be verified.
- Keep the application buildable at the end of each card where practical.
- Record design changes in `ARCHITECTURE.md` before implementing behavior that conflicts with it.

## Definition of Ready

A card is Ready when:

- Its outcome is clear.
- Its acceptance criteria are testable.
- Its dependencies are identified.
- It has a defined owner or work track.

## Definition of Done

A card is Done when:

- The implementation is complete.
- The project builds without warnings introduced by the card.
- Relevant unit or integration tests pass.
- Manual verification has been performed for user-visible behavior.
- Error handling is present for expected failure cases.
- Documentation or configuration has been updated when needed.

## Work tracks

The work is divided into two balanced tracks:

### Track A: platform and switching

Foundation, Core Audio integration, and switching rules.

### Track B: user experience

Configuration, tray controls, hotkeys, and the visual switcher.

Release testing and packaging are shared responsibilities.

## Cards

### Foundation

#### FND-01 — Create the solution and project structure

**Track:** A  
**Dependencies:** None

Create the .NET solution, WPF application project, test project, and initial source folders.

**Acceptance criteria:**

- The solution opens and builds on Windows.
- The application starts without showing an unwanted console window.
- The test project runs successfully, even if it contains only a smoke test.
- The project targets Windows desktop explicitly.

#### FND-02 — Define application contracts and models

**Track:** A  
**Dependencies:** FND-01

Define device, configuration, and service abstractions without tying the application layer to Core Audio or WPF.

**Acceptance criteria:**

- A device has a stable endpoint ID, display name, availability state, and optional user label.
- Configuration supports an ordered list of devices.
- Audio access is represented by an interface such as `IAudioDeviceService`.
- UI code is not required to construct Core Audio objects directly.

#### FND-03 — Add single-instance application behavior

**Track:** A  
**Dependencies:** FND-01

Prevent multiple copies of the tray application from running simultaneously.

**Acceptance criteria:**

- The first instance acquires a per-user mutex.
- A second launch exits cleanly.
- The design leaves room for a second launch to signal the existing instance later.

### Audio engine

#### AUD-01 — Enumerate output devices

**Track:** A  
**Dependencies:** FND-02

Implement the Core Audio adapter for output/render-device enumeration.

**Acceptance criteria:**

- Active output devices can be listed.
- Each device exposes a stable Windows endpoint ID.
- Device display names are available for setup and the overlay.
- Audio-library details do not leak outside the infrastructure adapter.

#### AUD-02 — Read the current default output

**Track:** A  
**Dependencies:** AUD-01

Expose the current Windows default output device through the audio service.

**Acceptance criteria:**

- The current default endpoint ID can be read.
- The result can be compared to configured endpoint IDs.
- Errors are returned in an application-friendly form.

#### AUD-03 — Set the default output

**Track:** A  
**Dependencies:** AUD-01

Implement changing the Windows default output endpoint.

**Acceptance criteria:**

- A connected output device can be made the Windows default.
- The operation does not require administrator privileges.
- A failed change does not falsely update application state.

#### AUD-04 — Handle device availability

**Track:** A  
**Dependencies:** AUD-01

Make disconnected and reconnected devices distinguishable from configured devices.

**Acceptance criteria:**

- A configured but disconnected device remains identifiable by endpoint ID.
- Re-enumeration detects when it becomes available again.
- The service does not substitute a different device for a missing one.

### Switching domain

#### SW-01 — Implement ordered device cycling

**Track:** A  
**Dependencies:** FND-02, AUD-01, AUD-02, AUD-04

Select the next enabled and available configured device, wrapping at the end of the ordered list.

**Acceptance criteria:**

- Any number of configured devices is supported.
- The current device is skipped when choosing the next device.
- Unavailable devices are skipped.
- Cycling wraps from the last configured device to the first.
- If the current output is not configured, the first available configured device is selected.

#### SW-02 — Implement direct device selection

**Track:** A  
**Dependencies:** SW-01, AUD-03

Allow application callers to select a specific configured device.

**Acceptance criteria:**

- A caller can select by endpoint ID.
- Unavailable devices cannot be selected.
- A successful selection updates the reported active device.
- Re-selecting the current device is harmless.

#### SW-03 — Serialize switching operations

**Track:** A  
**Dependencies:** SW-01, SW-02

Prevent rapid hotkey presses or UI actions from racing each other.

**Acceptance criteria:**

- Concurrent switch requests are serialized.
- The final application state matches the last completed operation.
- A failed operation does not leave a busy or locked state.

### Configuration

#### CFG-01 — Define the settings schema

**Track:** B  
**Dependencies:** FND-02

Define the versioned JSON settings format.

**Acceptance criteria:**

- The schema supports ordered devices, labels, enabled state, hotkey settings, startup preference, and schema version.
- New fields can be added without breaking older settings.
- The schema does not store transient device availability as authoritative state.

#### CFG-02 — Implement settings persistence

**Track:** B  
**Dependencies:** CFG-01

Load and save settings under the current user's application-data directory.

**Acceptance criteria:**

- Settings survive application restarts.
- Missing settings produce a valid first-run state.
- Malformed settings are recoverable without crashing the application.
- Invalid settings are backed up when practical before recreation.

#### CFG-03 — Build first-run configuration

**Track:** B  
**Dependencies:** AUD-01, CFG-02

Create a setup window where users choose, order, label, enable, and remove output devices.

**Acceptance criteria:**

- The window lists available output devices.
- Users can configure any number of devices.
- Users can reorder devices.
- Users can assign friendly labels.
- The configuration can be saved and reopened.
- Disconnected configured devices are visible and replaceable.

### Tray and keyboard control

#### UI-01 — Add the system-tray application

**Track:** B  
**Dependencies:** FND-01, SW-02, CFG-02

Add the tray icon, tooltip, and context menu.

**Acceptance criteria:**

- The application runs without a main window remaining open.
- The tooltip identifies the active output device.
- The menu lists configured devices.
- The menu supports direct selection, opening the switcher, reconfiguration, and exit.

#### UI-02 — Add the global hotkey

**Track:** B  
**Dependencies:** SW-01, UI-01

Register the default global hotkey and route it to the next-device command.

**Acceptance criteria:**

- The hotkey works while another application has focus.
- Repeated presses cycle through configured devices.
- The hotkey can be changed in configuration later.
- A registration conflict produces a clear user-facing message.
- The hotkey is released during shutdown.

#### UI-03 — Add startup-with-Windows support

**Track:** B  
**Dependencies:** UI-01

Allow the user to enable or disable launch at sign-in.

**Acceptance criteria:**

- Startup can be enabled and disabled from the application.
- The preference survives restarts.
- No administrator privileges are required.
- Uninstalling or removing the app does not leave an unexplained startup entry.

### Switcher overlay

#### OVR-01 — Create the visual switcher overlay

**Track:** B  
**Dependencies:** SW-02, UI-01

Create a compact borderless WPF overlay showing the configured device list.

**Acceptance criteria:**

- The active device is visually distinct.
- The selected device is visually distinct.
- Unavailable devices are clearly marked.
- The overlay appears quickly when invoked.
- The overlay dismisses after a short timeout and with Escape.

#### OVR-02 — Add overlay selection controls

**Track:** B  
**Dependencies:** OVR-01

Support mouse, arrow-key, Enter, and number-key selection.

**Acceptance criteria:**

- The overlay can select a specific available device.
- Arrow keys move selection predictably.
- Enter applies the highlighted device.
- Escape closes without changing the device.
- Number keys select directly where supported.

#### OVR-03 — Add switch feedback and error presentation

**Track:** B  
**Dependencies:** UI-01, OVR-01, SW-02

Show success, unavailable-device, and switching-error states consistently across the overlay, tray tooltip, and notifications.

**Acceptance criteria:**

- Successful switching identifies the selected device.
- Missing devices explain how to reconfigure.
- Failed switching does not claim success.
- Notifications do not prevent continued use of the application.

### Release and verification

#### REL-01 — Add unit and integration tests

**Track:** Shared  
**Dependencies:** SW-01, SW-02, CFG-02

Cover switching logic, configuration behavior, and Windows integration boundaries.

**Acceptance criteria:**

- Cycling tests cover one, two, and many configured devices.
- Tests cover unavailable devices and an unconfigured current default.
- Settings load/save and recovery are tested.
- Audio and hotkey integrations have manual or Windows-hosted verification steps.

#### REL-02 — Package the application

**Track:** Shared  
**Dependencies:** UI-03, OVR-03, REL-01

Produce a self-contained Windows build suitable for local use.

**Acceptance criteria:**

- A clean-machine or clean-profile launch test succeeds.
- The published build has no console window.
- Settings are stored in the expected per-user location.
- The application can be exited cleanly.
- Packaging instructions are recorded.

#### REL-03 — Perform end-to-end acceptance testing

**Track:** Shared  
**Dependencies:** REL-02

Verify the complete user workflow with multiple real audio devices.

**Acceptance criteria:**

- First-run setup works.
- Hotkey cycling works.
- Overlay selection works.
- Tray controls work.
- Device disconnect/reconnect behavior works.
- Startup behavior works.
- No known release-blocking errors remain.

#### REL-04 — Keep the audio path minimal and deterministic

**Track:** Shared  
**Dependencies:** AUD-03, REL-02

Use the bundled SoundVolumeCommandLine utility for Windows audio operations.
Keep the tray app as a single process and serialize external commands.

**Acceptance criteria:**

- The tray process invokes `svcl.exe` for enumeration and default-device
  mutation.
- NAudio, custom Windows audio-policy COM, and the audio worker are absent.
- External commands are serialized and have a bounded timeout.
- Non-zero exits, timeouts, and missing devices become user-facing switching
  errors.
- Successful switches do not emit a toast; repeated errors are throttled.
- Packaging places the unmodified `svcl.exe` beside the tray executable.

## Dependency summary

```text
FND-01
├── FND-02 ── AUD-01 ── AUD-02
│              └────── AUD-04
├── FND-03
└── CFG-01 ── CFG-02 ── CFG-03

AUD-02 + AUD-04 + FND-02 ── SW-01 ── SW-02 ── SW-03

SW-02 + CFG-02 ── UI-01 ── UI-02
                         ├── UI-03
                         └── OVR-01 ── OVR-02

UI-01 + OVR-01 + SW-02 ── OVR-03

SW-01 + SW-02 + CFG-02 ── REL-01
UI-03 + OVR-03 + REL-01 ── REL-02 ── REL-03
```

## Suggested first pull of cards

The next candidates for Ready are:

- CFG-02 — Implement settings persistence
- CFG-03 — Build first-run configuration

After those are complete, the audio and configuration tracks can proceed in parallel.

