# Packaging

The supported distribution is a self-contained portable folder. MSIX is not
used by the simplified build because the app needs to ship and invoke the
unmodified `svcl.exe` utility beside the tray executable.

From the repository root:

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location) '.nuget-packages'
dotnet publish .\src\SoundSwitcher\SoundSwitcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\artifacts\publish\simple-win-x64
```

Distribute the complete output directory, not only the main executable. The
bundled NirSoft files include the utility's license/readme and must remain
beside `SoundSwitcher.exe`.

The app does not require administrator privileges. Copy the output folder to a
stable user-owned location and run `SoundSwitcher.exe`.

Version changes follow SemVer. The current release is `1.1.0`.
