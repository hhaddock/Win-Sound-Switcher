# Sound Switcher

Small Windows 11 tray utility for changing the default output among any number
of configured audio devices. It supports a global hotkey, direct tray selection,
friendly labels, ordering, and optional launch-at-sign-in.

## Design

The app is one WPF/Windows Forms tray process. For Windows audio operations it
invokes the bundled `svcl.exe` from NirSoft's SoundVolumeCommandLine utility.
There is no custom Core Audio COM code, NAudio dependency, or audio worker
process in the supported build. The external utility is copied unmodified and
its license/readme are shipped beside it.

See the [official SoundVolumeCommandLine documentation](https://www.nirsoft.net/utils/sound_volume_command_line.html).

## Requirements

- Windows 11
- .NET 10 SDK for development
- At least one active audio output device

## Build and test

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location) '.nuget-packages'
dotnet restore .\SoundSwitcher.sln -r win-x64
dotnet build .\SoundSwitcher.sln --no-restore
dotnet test .\tests\SoundSwitcher.Tests\SoundSwitcher.Tests.csproj --no-restore
```

## Publish a portable app

```powershell
dotnet publish .\src\SoundSwitcher\SoundSwitcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\artifacts\publish\simple-win-x64
```

Keep these files together and run `SoundSwitcher.exe`:

- `SoundSwitcher.exe`
- `svcl.exe`
- `svcl.chm`
- `svcl-readme.txt`

The first launch opens configuration. Later launches stay in the notification
area. The default hotkey is `Ctrl+Alt+S`.

## Repository map

- `src/SoundSwitcher` — tray application, UI, persistence, and audio adapter
- `tests/SoundSwitcher.Tests` — automated tests
- `third-party/sound-volume-command-line` — unmodified NirSoft utility files
- `ARCHITECTURE.md` — design decisions
- `PACKAGING.md` — publish instructions
- `TESTING.md` — verification notes

Releases use SemVer. The current simplified release is `1.1.0`.
