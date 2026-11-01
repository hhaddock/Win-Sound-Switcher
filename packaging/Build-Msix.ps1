[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $Version = '1.0.0.0',

    [ValidatePattern('^[^<>]+$')]
    [string] $Publisher = 'CN=SoundSwitcher Development',

    [string] $OutputDirectory,

    [switch] $Sign,

    [string] $CertificatePath,

    [string] $CertificatePassword = 'SoundSwitcherDev'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$mainProject = Join-Path $repoRoot 'src\SoundSwitcher\SoundSwitcher.csproj'
$workerProject = Join-Path $repoRoot 'src\SoundSwitcher.AudioWorker\SoundSwitcher.AudioWorker.csproj'
$manifestTemplate = Join-Path $PSScriptRoot 'AppxManifest.xml'
$iconSource = Join-Path $repoRoot 'src\SoundSwitcher\Assets\sound-switcher.png'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\msix'
}

if (-not (Test-Path $mainProject)) { throw "Main project was not found: $mainProject" }
if (-not (Test-Path $workerProject)) { throw "Audio worker project was not found: $workerProject" }
if (-not (Test-Path $manifestTemplate)) { throw "Manifest template was not found: $manifestTemplate" }
if (-not (Test-Path $iconSource)) { throw "Application icon was not found: $iconSource" }

$makeAppx = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter 'makeappx.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\makeappx\.exe$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $makeAppx) {
    throw 'makeappx.exe was not found. Install the Windows 10/11 SDK, then run this script again.'
}

$outputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$stagingDirectory = Join-Path $outputDirectory 'staging'
$packagePath = Join-Path $outputDirectory "SoundSwitcher-$Version.msix"
$manifestPath = Join-Path $stagingDirectory 'AppxManifest.xml'

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
if (Test-Path $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

Write-Host 'Publishing the tray app and isolated audio worker...'
dotnet publish $mainProject -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $stagingDirectory
if ($LASTEXITCODE -ne 0) { throw 'The tray app publish failed.' }

dotnet publish $workerProject -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $stagingDirectory
if ($LASTEXITCODE -ne 0) { throw 'The audio worker publish failed.' }

$assetsDirectory = Join-Path $stagingDirectory 'Assets'
New-Item -ItemType Directory -Path $assetsDirectory -Force | Out-Null
foreach ($assetName in @('Square44x44Logo.png', 'Square150x150Logo.png')) {
    Copy-Item -LiteralPath $iconSource -Destination (Join-Path $assetsDirectory $assetName) -Force
}
Copy-Item -LiteralPath $iconSource -Destination (Join-Path $assetsDirectory 'StoreLogo.png') -Force

$publisherXml = [System.Security.SecurityElement]::Escape($Publisher)
$manifest = Get-Content -LiteralPath $manifestTemplate -Raw
$manifest = $manifest.Replace('__PUBLISHER__', $publisherXml).Replace('__VERSION__', $Version)
Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding UTF8

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Write-Host "Creating $packagePath..."
& $makeAppx.FullName pack /d $stagingDirectory /p $packagePath /o
if ($LASTEXITCODE -ne 0) { throw 'makeappx failed to create the MSIX package.' }

if ($Sign) {
    $signTool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if (-not $signTool) {
        throw 'signtool.exe was not found. Install the Windows 10/11 SDK, then run this script again.'
    }

    if (-not $CertificatePath) {
        $CertificatePath = Join-Path $outputDirectory 'SoundSwitcher-Development.pfx'
        $certificateFile = Join-Path $outputDirectory 'SoundSwitcher-Development.cer'
        $existingCertificate = Get-ChildItem 'Cert:\CurrentUser\My' |
            Where-Object { $_.Subject -eq $Publisher -and $_.HasPrivateKey } |
            Sort-Object NotAfter -Descending |
            Select-Object -First 1

        if (-not $existingCertificate) {
            Write-Host "Creating a development certificate for $Publisher..."
            $existingCertificate = New-SelfSignedCertificate `
                -Type Custom `
                -Subject $Publisher `
                -KeyUsage DigitalSignature `
                -FriendlyName 'Sound Switcher Development' `
                -CertStoreLocation 'Cert:\CurrentUser\My' `
                -NotAfter (Get-Date).AddYears(2)
        }

        $securePassword = ConvertTo-SecureString $CertificatePassword -AsPlainText -Force
        Export-PfxCertificate -Cert $existingCertificate -FilePath $CertificatePath -Password $securePassword | Out-Null
        Export-Certificate -Cert $existingCertificate -FilePath $certificateFile -Type CERT | Out-Null
        Write-Host "Development certificate exported to $certificateFile"
    }

    if (-not (Test-Path $CertificatePath)) {
        throw "Certificate was not found: $CertificatePath"
    }

    Write-Host 'Signing the MSIX package...'
    & $signTool.FullName sign /fd SHA256 /f $CertificatePath /p $CertificatePassword $packagePath
    if ($LASTEXITCODE -ne 0) { throw 'signtool failed to sign the MSIX package.' }
}

Write-Host ''
Write-Host "MSIX created: $packagePath"
if ($Sign) {
    Write-Host 'For local installation, trust the exported .cer for the current user, then run Add-AppxPackage.'
} else {
    Write-Host 'This package is unsigned. Re-run with -Sign for a locally installable development package.'
}
