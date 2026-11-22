$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "WiFiHotspotManager\WiFiHotspotManager.csproj"
$publish = Join-Path $PSScriptRoot "publish"

Write-Host "Checking .NET 8 SDK..."
dotnet --version
if ($LASTEXITCODE -ne 0) {
    throw ".NET 8 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/8.0"
}

Write-Host "Building self-contained Windows x64 executable..."
dotnet publish $project -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publish

if ($LASTEXITCODE -ne 0) { throw "Build failed. See errors above." }

Write-Host "Build complete: $publish\WiFiHotspotManager.exe"
