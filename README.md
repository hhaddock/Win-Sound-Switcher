# Sound Switcher

Sound Switcher is a Windows 11 tray utility for quickly changing the default
audio output among any number of configured devices. It supports ordered device
cycling, direct selection, a global hotkey, a visual overlay, and optional
launch-at-sign-in.

## Current status

This is an early, Windows-specific project. The current implementation uses
WPF, Windows Forms tray integration, .NET 10, and `NAudio.Wasapi`.

The release build contains two executables. `SoundSwitcher.exe` owns the tray
and UI. `SoundSwitcher.AudioWorker.exe` performs the risky Windows audio-policy
mutation in a separate process, so a native audio failure does not take down the
tray application. Keep both files in the same directory.

## Requirements

- Windows 11
- .NET 10 SDK for development
- At least one active audio output device

## Build and test

From the repository root:

```powershell
dotnet restore .\SoundSwitcher.sln
dotnet build .\SoundSwitcher.sln --no-restore
dotnet test .\tests\SoundSwitcher.Tests\SoundSwitcher.Tests.csproj --no-restore
```

The integration tests use the current Windows audio endpoints. The switching
test restores the original default output in a `finally` block.

## Publish a portable build

Publish both projects to the same directory:

```powershell
$output = Join-Path (Get-Location) 'artifacts\publish\win-x64'

dotnet publish .\src\SoundSwitcher\SoundSwitcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $output

dotnet publish .\src\SoundSwitcher.AudioWorker\SoundSwitcher.AudioWorker.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $output
```

Run the tray application with:

```powershell
Start-Process .\artifacts\publish\win-x64\SoundSwitcher.exe
```

The first launch opens configuration. Later launches run in the notification
area. Use `Ctrl+Alt+S` to advance to the next enabled output, or right-click the
tray icon for direct selection and configuration.

## Repository map

- `src/SoundSwitcher` — tray application, UI, domain, persistence, and audio adapter
- `src/SoundSwitcher.AudioWorker` — isolated audio-policy worker
- `tests/SoundSwitcher.Tests` — unit and Windows-hosted integration tests
- `ARCHITECTURE.md` — design and fault-boundary decisions
- `KANBAN.md` — implementation cards and acceptance criteria
- `PACKAGING.md` — release packaging details
- `TESTING.md` — automated and manual verification notes

## Known limitations

- Windows-only; the audio policy interface is an undocumented Windows boundary.
- Physical disconnect/reconnect and sign-in startup verification still require
  manual testing on the target machine.
- No installer or code-signing certificate is included yet.
