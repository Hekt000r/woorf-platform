param([Parameter(Mandatory=$true)][string]$Project, [Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path -LiteralPath $Project).Path
if ((Get-Item -LiteralPath $projectPath).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked project paths are unsupported.' }
$manifestPath = Join-Path $projectPath 'woorf.project.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$root = if ($manifest.root) { $manifest.root } else { 'src' }
$sourcePath = [IO.Path]::GetFullPath((Join-Path $projectPath $root))
$relative = [IO.Path]::GetRelativePath($projectPath, $sourcePath)
if ([IO.Path]::IsPathRooted($relative) -or $relative -eq '..' -or $relative.StartsWith('../') -or $relative.StartsWith('..\')) {
    throw 'Project root must remain inside the project directory.'
}
if (-not (Test-Path -LiteralPath $sourcePath -PathType Container)) { throw 'Project source root does not exist.' }
$files = [ordered]@{ 'woorf.project.json' = [IO.File]::ReadAllText($manifestPath) }
foreach ($entry in Get-ChildItem -LiteralPath $sourcePath -Recurse -Force) {
    if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked source paths are unsupported.' }
    if ($entry.PSIsContainer -or ($entry.Extension -ne '.luau' -and $entry.Name -ne 'init.meta.json')) { continue }
    $path = [IO.Path]::GetRelativePath($projectPath, $entry.FullName).Replace('\', '/')
    $files[$path] = [IO.File]::ReadAllText($entry.FullName)
}
$outputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath)) | Out-Null
[IO.File]::WriteAllText($outputPath, (@{ files = $files } | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
