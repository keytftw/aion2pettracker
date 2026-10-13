param(
    [string]$VpkPath,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$VpkPath) { $VpkPath = Join-Path $projectRoot '.tools/vpk/vpk.exe' }
if (!(Test-Path -LiteralPath $VpkPath)) { throw 'Install the pinned build tool: dotnet tool install vpk --version 1.2.161 --tool-path .tools/vpk' }
[xml]$appProject = Get-Content (Join-Path $projectRoot 'src/PetTracker.App/PetTracker.App.csproj')
[xml]$setupProject = Get-Content (Join-Path $projectRoot 'src/PetTracker.Setup/PetTracker.Setup.csproj')
$releaseVersion = @($appProject.Project.PropertyGroup.Version | Where-Object { $_ })[0]
$setupVersion = @($setupProject.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if ($releaseVersion -notmatch '^\d+\.\d+\.\d+$' -or $setupVersion -ne $releaseVersion) { throw 'App and Setup versions must match.' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot "releases/installer-$releaseVersion" }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (!$OutputDirectory.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be within this project.' }
$buildRoot = Join-Path $projectRoot ".build/setup-$releaseVersion"
$appOutput = Join-Path $buildRoot 'app'
$wizardOutput = Join-Path $buildRoot 'wizard'
$payload = Join-Path $buildRoot 'InstallPayload.exe'
$payloadHash = Join-Path $buildRoot 'InstallPayload.sha256'
New-Item -ItemType Directory -Force -Path $buildRoot, $OutputDirectory | Out-Null
# A single MSBuild node avoids child-node failures in restricted Windows build environments.
dotnet publish (Join-Path $projectRoot 'src/PetTracker.App/PetTracker.App.csproj') -c Release -r win-x64 --self-contained true -m:1 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded -o $appOutput
if ($LASTEXITCODE) { throw 'Application publish failed.' }
foreach ($required in @('data/pet-catalog.json','data/servers.json','data/PET-SOURCES.md','LICENSE','THIRD_PARTY_NOTICES.md')) {
    if (!(Test-Path -LiteralPath (Join-Path $appOutput $required))) { throw "Published application file is missing: $required" }
}
foreach ($assetDirectory in @('data/pets','licenses')) {
    foreach ($asset in Get-ChildItem -LiteralPath (Join-Path $projectRoot $assetDirectory) -File) {
        if (!(Test-Path -LiteralPath (Join-Path $appOutput ($assetDirectory + '/' + $asset.Name)))) { throw "Published asset is missing: $assetDirectory/$($asset.Name)" }
    }
}
$mapMetadata = Join-Path $appOutput 'data/maps/spawns.json'
if (!(Test-Path -LiteralPath $mapMetadata)) { throw 'Published map metadata is missing.' }
$mapData = Get-Content -LiteralPath $mapMetadata -Raw | ConvertFrom-Json
foreach ($world in $mapData.worlds.PSObject.Properties) {
    $key = $world.Value.key
    if ($key -notmatch '^[A-Za-z0-9_]+$') { throw 'Invalid map image key.' }
    if (!(Test-Path -LiteralPath (Join-Path $appOutput ('data/maps/' + $key + '.png')))) { throw "Published map image is missing: $key" }
}
# Older downloaded feed folders may contain a portable ZIP. Do not publish it again.
Get-ChildItem -LiteralPath $OutputDirectory -File -Filter '*-Portable.zip' | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
# The reviewed fixed-version packaging implementation runs an app compatibility probe unless this switch is set.
# Skip that probe: this build script never runs the tracker or an installer.
& $VpkPath pack --packId A2PetTracker --packVersion $releaseVersion --packDir $appOutput --mainExe A2PetTracker.exe --packTitle A2PetTracker --runtime win-x64 --noPortable --shortcuts StartMenuRoot --skipVeloAppCheck --icon (Join-Path $projectRoot 'src/PetTracker.App/Assets/App.ico') --outputDir $OutputDirectory
if ($LASTEXITCODE) { throw 'Velopack packaging failed.' }
$publicSetup = Join-Path $OutputDirectory 'A2PetTracker-win-Setup.exe'
if (!(Test-Path -LiteralPath $publicSetup)) { throw 'Expected Velopack installer was not produced.' }
Copy-Item -LiteralPath $publicSetup -Destination $payload -Force
(Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash | Set-Content -LiteralPath $payloadHash -Encoding ascii
dotnet publish (Join-Path $projectRoot 'src/PetTracker.Setup/PetTracker.Setup.csproj') -c Release -r win-x64 --self-contained true -m:1 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded "-p:InstallerPayload=$payload" "-p:InstallerPayloadHash=$payloadHash" -o $wizardOutput
if ($LASTEXITCODE) { throw 'Custom Setup publish failed.' }
Copy-Item -LiteralPath (Join-Path $wizardOutput 'A2PetTracker-win-Setup.exe') -Destination $publicSetup -Force
if (Get-ChildItem -LiteralPath $OutputDirectory -File -Filter '*-Portable.zip') { throw 'Portable artifacts must not be published.' }
Write-Output "Setup built (not executed): $publicSetup"
