# PowerShell script to prepare the package folder structure on Windows
$RootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$publishDir = Join-Path $RootDir "linux\publish-arm64"
if (Test-Path $publishDir) {
    $BuildDir = $publishDir
} else {
    $BuildDir = Join-Path $RootDir "linux\bin\Release\net8.0\linux-arm64\publish"
}

$PkgRoot = Join-Path $RootDir "linux\packaging\tepi_arm64"

Write-Host "Setting up Debian package directory at: $PkgRoot" -ForegroundColor Cyan
Write-Host "Using build output from: $BuildDir" -ForegroundColor Cyan

if (Test-Path $PkgRoot) { Remove-Item -Path $PkgRoot -Recurse -Force }

New-Item -ItemType Directory -Path "$PkgRoot\DEBIAN" -Force | Out-Null
New-Item -ItemType Directory -Path "$PkgRoot\usr\bin" -Force | Out-Null
New-Item -ItemType Directory -Path "$PkgRoot\opt\tepi" -Force | Out-Null
New-Item -ItemType Directory -Path "$PkgRoot\usr\share\applications" -Force | Out-Null
New-Item -ItemType Directory -Path "$PkgRoot\usr\share\icons\hicolor\256x256\apps" -Force | Out-Null

Copy-Item "$RootDir\linux\packaging\DEBIAN\control" "$PkgRoot\DEBIAN\control"
Copy-Item "$BuildDir\*" "$PkgRoot\opt\tepi\" -Recurse

$launcherContent = @"
#!/bin/sh
export DOTNET_GCHeapHardLimit=20000000
export DOTNET_gcServer=0
export DOTNET_EnableWriteXorExecute=0
export LD_LIBRARY_PATH="/opt/tepi:`$LD_LIBRARY_PATH"
cd /opt/tepi
exec /opt/tepi/Tepi.Linux "`$@"
"@
[System.IO.File]::WriteAllText("$PkgRoot\usr\bin\tepi", $launcherContent.Replace("`r`n", "`n"))

Copy-Item "$RootDir\linux\packaging\tepi.desktop" "$PkgRoot\usr\share\applications\tepi.desktop"

Write-Host "Package directory ready (/opt/tepi layout)." -ForegroundColor Green
