param([string]$Project = 'examples/obby', [string]$Output = 'artifacts/browser', [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$projectPath = (Resolve-Path -LiteralPath $Project).Path
$outputPath = [IO.Path]::GetFullPath($Output)
$dotnetRoot = Split-Path -Parent (Get-Command dotnet).Source
$packs = Join-Path $dotnetRoot 'packs'
function Find-Pack([string]$name) {
    $directory = Join-Path $packs $name
    $version = Get-ChildItem -LiteralPath $directory -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if (-not $version) { throw "Missing $name. Install the .NET 9 wasm-tools workload first." }
    return Join-Path $version.FullName 'tools'
}
$sdk = Find-Pack 'Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64'
$env:EMSDK_PYTHON = Join-Path (Find-Pack 'Microsoft.NET.Runtime.Emscripten.3.1.56.Python.win-x64') 'python.exe'
$env:DOTNET_EMSCRIPTEN_LLVM_ROOT = Join-Path $sdk 'bin'
$env:DOTNET_EMSCRIPTEN_BINARYEN_ROOT = $sdk
$env:DOTNET_EMSCRIPTEN_NODE_JS = Join-Path (Find-Pack 'Microsoft.NET.Runtime.Emscripten.3.1.56.Node.win-x64') 'bin/node.exe'
$env:EM_CACHE = Join-Path (Find-Pack 'Microsoft.NET.Runtime.Emscripten.3.1.56.Cache.win-x64') 'emscripten/cache'
$env:PATH = (Split-Path $env:EMSDK_PYTHON) + ';' + (Join-Path $sdk 'emscripten') + ';' + $env:PATH
$nativeBuild = Join-Path $repo 'build/browser-native'
& $env:EMSDK_PYTHON (Join-Path $sdk 'emscripten/emcmake.py') cmake -S (Join-Path $repo 'native') -B $nativeBuild -G Ninja -DCMAKE_BUILD_TYPE=Release
if ($LASTEXITCODE -ne 0) { throw 'Browser native configuration failed.' }
cmake --build $nativeBuild --target woorf_luau --parallel 4
if ($LASTEXITCODE -ne 0) { throw 'Browser native compilation failed.' }
dotnet publish (Join-Path $repo 'src/Woorf.Browser') -c $Configuration --configfile (Join-Path $repo 'NuGet.Config') -o $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Browser publishing failed.' }
$webRoot = Join-Path $outputPath 'wwwroot'
& (Join-Path $PSScriptRoot 'export-browser-game.ps1') -Project $projectPath -Output (Join-Path $webRoot 'game.json')
Write-Output "Browser game published to $webRoot. Serve this directory over HTTP."
