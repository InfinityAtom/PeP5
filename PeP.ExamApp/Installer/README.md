# PeP Exam App Installer

This folder contains the installer build system for PeP Exam App.

## Prerequisites

1. **Inno Setup 6** - Download and install from [https://jrsoftware.org/isinfo.php](https://jrsoftware.org/isinfo.php)
2. **.NET 8 SDK** - Required for building the application

## Building the Installer

### Quick Build

Open PowerShell in this folder and run:

```powershell
.\build-installer.ps1 -Version "1.0.0"
```

### Build Options

```powershell
# Build with specific version
.\build-installer.ps1 -Version "1.2.3"

# Skip publish step (use existing publish output)
.\build-installer.ps1 -Version "1.2.3" -SkipPublish

# Skip installer creation (only publish)
.\build-installer.ps1 -Version "1.2.3" -SkipInstaller

# Use Debug configuration
.\build-installer.ps1 -Version "1.2.3" -Configuration Debug
```

## Output

After building, you'll find:
- `Output/PeP.ExamApp_Setup_X.X.X.exe` - The installer
- `Output/PeP.ExamApp_Setup_X.X.X.sha256` - SHA256 checksum file

## Installer Features

- **Silent Installation**: Run with `/SILENT` or `/VERYSILENT` flags
- **Custom Directory**: Users can choose installation location
- **Desktop Shortcut**: Optional desktop icon
- **Start Menu**: Creates program group
- **Auto-Restart**: After update with `/RESTARTAPPLICATIONS` flag
- **Clean Uninstall**: Removes all files and optionally app data

## Silent Installation Commands

```cmd
# Standard silent install
PeP.ExamApp_Setup_1.0.0.exe /SILENT

# Very silent (no UI at all)
PeP.ExamApp_Setup_1.0.0.exe /VERYSILENT

# Silent install to specific directory
PeP.ExamApp_Setup_1.0.0.exe /SILENT /DIR="C:\MyApps\PeP"

# Silent install with restart
PeP.ExamApp_Setup_1.0.0.exe /SILENT /RESTARTAPPLICATIONS
```

## Customization

### Wizard Images

Place these files in this folder to customize the installer appearance:
- `WizardImage.bmp` - 164x314 pixels, left panel image
- `WizardSmallImage.bmp` - 55x58 pixels, top-right corner image

### Code Signing

To sign the installer for production, uncomment and configure the `SignTool` line in `PeP.ExamApp.iss`:

```inno
SignTool=signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a $f
```

## Update Server Setup

For auto-updates to work, your web server needs an endpoint that returns update info:

**Endpoint:** `GET /api/examapp/update?currentVersion=1.0.0`

**Response:**
```json
{
  "version": "1.1.0",
  "downloadUrl": "https://yourserver.com/downloads/PeP.ExamApp_Setup_1.1.0.exe",
  "releaseNotes": "- Bug fixes\n- Performance improvements",
  "releaseDate": "2025-12-22T00:00:00Z",
  "isMandatory": false,
  "minimumVersion": "1.0.0",
  "checksum": "sha256hashhere"
}
```

Return empty/null or HTTP 204 if no update is available.

## Server Configuration

The PeP web app has the update endpoint already configured. To update the version info, edit `appsettings.json`:

```json
{
  "ExamAppUpdate": {
    "Version": "1.1.0",
    "DownloadUrl": "https://yourserver.com/downloads/PeP.ExamApp_Setup_1.1.0.exe",
    "ReleaseNotes": "- Bug fixes\n- Performance improvements",
    "ReleaseDate": "2025-12-22",
    "IsMandatory": false,
    "MinimumVersion": "1.0.0",
    "Checksum": "sha256hashofinstallerfile"
  }
}
```

## Deployment Workflow

1. Update version in `PeP.ExamApp.csproj`
2. Build installer: `.\build-installer.ps1 -Version "X.X.X"`
3. Upload installer to your download server
4. Update `appsettings.json` with new version info and checksum
5. Restart the PeP web server

The ExamApp will check for updates on startup and prompt users to install.
