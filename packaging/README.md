# MSIX packaging

`Build-Msix.ps1` creates a Windows MSIX package containing both executables:

- `SoundSwitcher.exe` — the tray application and Start-menu entry point.
- `SoundSwitcher.AudioWorker.exe` — the isolated audio-switching process.

Build an unsigned package:

```powershell
.\packaging\Build-Msix.ps1
```

Build and sign a development package:

```powershell
.\packaging\Build-Msix.ps1 -Sign
```

The signed command creates a development certificate under `artifacts\msix`. For a local install, trust `SoundSwitcher-Development.cer` for the current user, then install the package:

```powershell
Import-Certificate .\artifacts\msix\SoundSwitcher-Development.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
Add-AppxPackage .\artifacts\msix\SoundSwitcher-1.0.1.0.msix
```

After installation, search for **Sound Switcher** from Start. To remove it:

```powershell
Get-AppxPackage -Name SoundSwitcher | Remove-AppxPackage
```

The development certificate is only for local sideloading. A distributable release needs a certificate issued for the publisher identity, or Microsoft Store packaging/signing.

## Versioning

Use SemVer for releases: `MAJOR.MINOR.PATCH`. MSIX requires four numeric
components, so the package maps `1.0.1` to `1.0.1.0`. Every changed package
must increase the version; never rebuild different contents as the same MSIX
version.
