# Sound Switcher testing

## Automated checks

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location) '.nuget-packages'
dotnet restore .\SoundSwitcher.sln -r win-x64
dotnet test .\tests\SoundSwitcher.Tests\SoundSwitcher.Tests.csproj --no-restore
dotnet build .\SoundSwitcher.sln --no-restore
```

## Manual smoke test

1. Publish the portable build using `PACKAGING.md`.
2. Confirm `SoundSwitcher.exe` and `svcl.exe` are beside each other.
3. Launch `SoundSwitcher.exe` and configure at least two active outputs.
4. Switch from the tray menu, then switch back.
5. Use the global hotkey repeatedly and confirm the device changes without
   duplicate notifications or additional processes remaining behind.
6. Close and relaunch the app; confirm configuration is retained.

## Verified during the simplification

- `svcl.exe /SetDefault ... all` switched between the current machine's
  speakers and headphones and returned exit code 0 in both directions.
- The simplified solution passed 30 tests with 0 failures.
- Release publish produced the app and all three required bundled utility
  files in the output directory.
