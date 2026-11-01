# Sound Switcher testing

## Automated checks

From the repository root, configure the local .NET CLI and package locations if needed:

~~~powershell
$TaskDotnetHome = Join-Path (Get-Location) '.dotnet-cli'
$TaskNugetPackages = Join-Path (Get-Location) '.nuget-packages'
$env:DOTNET_CLI_HOME = $TaskDotnetHome
$env:NUGET_PACKAGES = $TaskNugetPackages
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet build .\SoundSwitcher.sln
dotnet test .\tests\SoundSwitcher.Tests\SoundSwitcher.Tests.csproj --no-build --no-restore
~~~

The Windows-hosted audio tests enumerate the current machine's active render
endpoints and temporarily switch the Multimedia default when the test is run.
They restore the original endpoint in a finally block.

## Verified on the current Windows host

- The published executable launched as a tray-only process with nine configured
  output rows.
- Ctrl+Alt+S opened the overlay with nine device rows.
- Escape closed the overlay and the process remained usable.
- A second published launch exited with code 0 while the first instance stayed
  alive.
- The automated Windows audio tests passed and restore the original default
  endpoint after switching.
- The resilient published build launches with `SoundSwitcher.AudioWorker.exe`
  beside the tray executable; an invalid worker request exits with code 1 while
  the tray process remains alive. The worker's `list` operation successfully
  enumerated nine active render endpoints after the full audio boundary was
  isolated.

## Manual Windows verification

1. Start the application from the built executable.
2. Confirm the first-run configuration window lists the machine's output devices.
3. Configure at least two devices, reorder them, rename one, disable one, and save.
4. Confirm the app remains in the system tray and its tooltip names the active output.
5. Use the tray menu to switch directly to each connected configured device.
6. Press Ctrl+Alt+S from another application and confirm the next enabled device becomes the Windows default.
7. Open the switcher overlay and verify active, available, and unavailable states.
8. Use arrow keys, number keys, Enter, mouse double-click, and Escape.
9. Disconnect a configured device and confirm it remains visible and is skipped.
10. Enable launch-at-sign-in, restart Windows or sign out/in, then disable it and confirm the per-user Run entry is removed.
11. Attempt to launch a second copy and confirm it exits without creating a second tray icon.
