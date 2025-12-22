# PeP Exam App Installer Build Script
# Prerequisites: 
#   - Inno Setup installed (https://jrsoftware.org/isinfo.php)
#   - .NET 8 SDK

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Split-Path -Parent $ScriptDir
$SolutionDir = Split-Path -Parent $ProjectDir
$PublishDir = Join-Path $ProjectDir "bin\Publish"
$OutputDir = Join-Path $ScriptDir "Output"
$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Colors for output
function Write-Step { param($msg) Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "   $msg" -ForegroundColor Green }
function Write-Info { param($msg) Write-Host "   $msg" -ForegroundColor Gray }

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  PeP Exam App Installer Builder" -ForegroundColor Yellow
Write-Host "  Version: $Version" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

# Step 1: Update version in project file
Write-Step "Updating version to $Version"
$csprojPath = Join-Path $ProjectDir "PeP.ExamApp.csproj"
$csproj = Get-Content $csprojPath -Raw

if ($csproj -notmatch "<Version>") {
    $csproj = $csproj -replace "(<PropertyGroup>)", "`$1`n    <Version>$Version</Version>`n    <AssemblyVersion>$Version.0</AssemblyVersion>`n    <FileVersion>$Version.0</FileVersion>"
} else {
    $csproj = $csproj -replace "<Version>.*?</Version>", "<Version>$Version</Version>"
    $csproj = $csproj -replace "<AssemblyVersion>.*?</AssemblyVersion>", "<AssemblyVersion>$Version.0</AssemblyVersion>"
    $csproj = $csproj -replace "<FileVersion>.*?</FileVersion>", "<FileVersion>$Version.0</FileVersion>"
}
$csproj | Set-Content $csprojPath -NoNewline
Write-Success "Version updated in csproj"

# Step 2: Publish the application
if (-not $SkipPublish) {
    Write-Step "Publishing application..."
    
    # Clean previous publish
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }
    
    # Publish
    Push-Location $ProjectDir
    dotnet publish -c $Configuration -r win-x64 --self-contained true -o $PublishDir -p:PublishReadyToRun=true
    Pop-Location
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    
    Write-Success "Application published to $PublishDir"
    
    # Show published files
    $files = Get-ChildItem $PublishDir -Recurse -File
    $totalSize = ($files | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Info "Published $($files.Count) files, total size: $([math]::Round($totalSize, 2)) MB"
} else {
    Write-Info "Skipping publish step"
}

# Step 3: Update Inno Setup script version
Write-Step "Updating installer script version..."
$issPath = Join-Path $ScriptDir "PeP.ExamApp.iss"
$issContent = Get-Content $issPath -Raw
$issContent = $issContent -replace '#define MyAppVersion ".*?"', "#define MyAppVersion `"$Version`""
$issContent | Set-Content $issPath -NoNewline
Write-Success "Installer script version updated"

# Step 4: Build installer
if (-not $SkipInstaller) {
    Write-Step "Building installer..."
    
    # Check for Inno Setup
    if (-not (Test-Path $InnoSetupPath)) {
        # Try alternate paths
        $alternatePaths = @(
            "C:\Program Files\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
        )
        foreach ($path in $alternatePaths) {
            if (Test-Path $path) {
                $InnoSetupPath = $path
                break
            }
        }
    }
    
    if (-not (Test-Path $InnoSetupPath)) {
        Write-Host ""
        Write-Host "ERROR: Inno Setup not found!" -ForegroundColor Red
        Write-Host "Please install Inno Setup from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "After installing, run this script again." -ForegroundColor Gray
        exit 1
    }
    
    # Create output directory
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }
    
    # Build installer
    & $InnoSetupPath $issPath
    
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup build failed with exit code $LASTEXITCODE"
    }
    
    # Show output
    $installer = Get-ChildItem (Join-Path $OutputDir "*.exe") | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($installer) {
        $size = [math]::Round($installer.Length / 1MB, 2)
        Write-Success "Installer created: $($installer.Name) ($size MB)"
        
        # Generate checksum
        $hash = Get-FileHash $installer.FullName -Algorithm SHA256
        $checksumFile = Join-Path $OutputDir "$($installer.BaseName).sha256"
        "$($hash.Hash.ToLower())  $($installer.Name)" | Set-Content $checksumFile
        Write-Success "Checksum: $($hash.Hash.ToLower())"
    }
} else {
    Write-Info "Skipping installer build step"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $OutputDir" -ForegroundColor Gray
Write-Host ""
