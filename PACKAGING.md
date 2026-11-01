# Sound Switcher packaging

## Publish a portable self-contained build

From the repository root, run:

~~~powershell
$TaskDotnetHome = Join-Path (Get-Location) '.dotnet-cli'
$TaskNugetPackages = Join-Path (Get-Location) '.nuget-packages'
$env:DOTNET_CLI_HOME = $TaskDotnetHome
$env:NUGET_PACKAGES = $TaskNugetPackages
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet publish .\src\SoundSwitcher\SoundSwitcher.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  --output .\artifacts\publish\win-x64
~~~

The resilient build also requires the audio worker beside the main executable:

~~~powershell
dotnet publish .\src\SoundSwitcher\SoundSwitcher.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output .\artifacts\publish\win-x64

dotnet publish .\src\SoundSwitcher.AudioWorker\SoundSwitcher.AudioWorker.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output .\artifacts\publish\win-x64
~~~

`SoundSwitcher.AudioWorker.exe` must remain in the same directory as
`SoundSwitcher.exe`. The tray process uses the worker for the Windows audio
policy mutation so a native audio failure cannot terminate the tray UI.

Copy the resulting SoundSwitcher.exe to a stable user-owned folder and launch
it directly. The application stores settings under the current user's
ApplicationData folder and does not require administrator privileges.

The published executable is a Windows GUI application and does not open a
console window. Remove the per-user startup setting from the configuration
window before deleting the executable if launch-at-sign-in was enabled.
