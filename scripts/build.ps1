# 构建「随时存档」Mod，并把产物复制到游戏目录的 BepInEx/plugins。
#
# 用法：
#   pwsh -File scripts/build.ps1
#   pwsh -File scripts/build.ps1 -GameDir 'D:\steam\steamapps\common\Ori and the Will of the Wisps'

param(
    [string]$GameDir = $env:ORIWOTW_DIR
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\SaveAnywhere\SaveAnywhere.csproj'

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = 'D:\steam\steamapps\common\Ori and the Will of the Wisps'
}
$GameDir = $GameDir.TrimEnd('\')

if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx\interop'))) {
    throw "游戏目录里没有找到 BepInEx\interop，请先确认 BepInEx 6 (IL2CPP) 已装好：$GameDir"
}

Write-Host "游戏目录: $GameDir"
dotnet build $project -c Release -p:GameDir="$GameDir\"

$built = Join-Path $repoRoot 'src\SaveAnywhere\bin\Release\SaveAnywhere.dll'
$target = Join-Path $GameDir 'BepInEx\plugins\SaveAnywhere.dll'
Copy-Item -LiteralPath $built -Destination $target -Force
Write-Host "已复制到: $target"
