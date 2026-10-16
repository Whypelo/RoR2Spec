# Вспомогательный скрипт сборки RoR2FirstPersonBrawler.
# Ищет установленную Risk of Rain 2 и печатает в stdout ПУТЬ К ПАПКЕ ИГРЫ
# (одной строкой) либо ничего, если игра не найдена.
# Используется target'ом AutoDetectRoR2Path из csproj: MSBuild присваивает
# вывод этому свойству безусловно — без хрупких MSBuild-условий с Exists(),
# которые вычислялись до автоопределения пути и приводили к массовым CS0246.
$ErrorActionPreference = 'SilentlyContinue'

function Test-RoR2([string]$root) {
    if ([string]::IsNullOrWhiteSpace($root)) { return $false }
    $a = Join-Path $root 'steamapps\common\Risk of Rain 2'
    if (Test-Path -LiteralPath (Join-Path $a 'Risk of Rain 2_Data\Managed\Assembly-CSharp.dll')) {
        Write-Output $a
        return $true
    }
    # вариант: root уже является папкой игры (пользователь задал путь к игре напрямую)
    if (Test-Path -LiteralPath (Join-Path $root 'Risk of Rain 2_Data\Managed\Assembly-CSharp.dll')) {
        Write-Output $root.TrimEnd('\')
        return $true
    }
    return $false
}

$candidates = @()
$v = Get-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -Name InstallPath
if ($v -and $v.InstallPath) { $candidates += $v.InstallPath }
$v2 = Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath
if ($v2 -and $v2.SteamPath) { $candidates += $v2.SteamPath }
$candidates = $candidates | Select-Object -Unique

foreach ($s in $candidates) {
    if (Test-RoR2 $s) { exit 0 }
}

# Дополнительные библиотеки Steam из libraryfolders.vdf (игра может стоять
# на другом диске, например F:\SteamLibrary).
foreach ($s in $candidates) {
    $vdf = Join-Path $s 'steamapps\libraryfolders.vdf'
    if (-not (Test-Path -LiteralPath $vdf)) { continue }
    foreach ($line in (Get-Content -LiteralPath $vdf)) {
        if ($line -match '"path"\s+"(.+?)"') {
            $lib = $matches[1] -replace '\\\\', '\'
            if (Test-RoR2 $lib) { exit 0 }
        }
    }
}

exit 0
