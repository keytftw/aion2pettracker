param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(!$OutputDirectory){$OutputDirectory=Join-Path $projectRoot 'releases/LOCAL-TEST'}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(!$OutputDirectory.StartsWith($projectRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Test output must be inside this workspace.'}
dotnet publish (Join-Path $projectRoot 'src/PetTracker.App/PetTracker.App.csproj') -c Release -r win-x64 --self-contained true -m:1 -p:LocalTestBuild=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded -o $OutputDirectory
if($LASTEXITCODE){throw 'Local test publish failed. Close the running test application before rebuilding.'}
foreach($required in @('A2PetTracker.exe','data/pet-catalog.json','data/pet-sources.json','data/servers.json','data/maps/spawns.json','LICENSE','THIRD_PARTY_NOTICES.md'))
{if(!(Test-Path -LiteralPath (Join-Path $OutputDirectory $required))){throw "Missing test file: $required"}}
$instructions=@'
YEREL TEST SURUMU - KURULUM GEREKMEZ

1. Acik normal tracker'i veya onceki test surumunu kapat.
2. Bu klasordeki A2PetTracker.exe dosyasini calistir.
3. EXE'yi tek basina tasima: data ve licenses klasorleri yaninda kalmali.
4. Ayarlar > Soul Test ile test raporunu kaydet.

Ilk acilista dil ve kosullari sec. Sonraki acilislarda secimlerin saklanir.
Bu surum otomatik guncelleme yapmaz, kurulu uygulamanin kayitlarini kullanmaz.
Test kayitlari: %LOCALAPPDATA%\Aion2PetTracker\LocalTest
Sonraki surumler icin sadece test uygulamasini kapatip yeniden acman yeterli.
NpCap mevcut kurulumunu kullanir. Bu paket bir installer degildir.
'@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'TEST-OKU.txt'),$instructions)
Write-Output "Local test built (not executed): $(Join-Path $OutputDirectory 'A2PetTracker.exe')"
