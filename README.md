# RoR2Spec
RoR2 Specter Mod Spec

## Сборка (Visual Studio 2026 Insiders)

Проект адаптирован под VS2026: используется SDK-style `.csproj` (TargetFramework `net35`, LangVersion `12`).

1. Установите в VS2026 workload **.NET desktop development** (legacy-компонент «.NET Framework 3.5 targeting pack» больше не требуется — net35-референсы берутся из reference-асsemblies NuGet-пакета `Microsoft.NETFramework.ReferenceAssemblies.net35` при restore, либо можно указать свой путь через `RoR2_PATH`).
2. Задайте путь к игре одним из способов:
   - переменная окружения `RoR2_PATH` (например, `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2`);
   - или файл `RoR2FirstPersonBrawler/RoR2FirstPersonBrawler/RoR2FirstPersonBrawler.csproj.user` (не отслеживается git):
     ```xml
     <Project>
       <PropertyGroup>
         <RoR2Path>C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2</RoR2Path>
       </PropertyGroup>
     </Project>
     ```
3. Откройте `RoR2FirstPersonBrawler/RoR2FirstPersonBrawler.sln`, выполните **Restore → Build**. После сборки DLL и `manifest.json` автоматически копируются в `<игра>\BepInEx\plugins\RoR2FirstPersonBrawler\`.

### Ручное подключение зависимостей (рекомендуется, если путь к игре не определяется)

Проект поддерживает режим автономных ссылок: создайте папку `lib` рядом с csproj
(`RoR2FirstPersonBrawler\RoR2FirstPersonBrawler\lib\`) и скопируйте в неё DLL.
Все `.dll` из `lib\` подхватываются как ссылки автоматически, а ссылки по пути к
игре в этом режиме отключаются — `RoR2_PATH` задавать не нужно вообще.

Минимальный набор файлов (из Steam-папки игры):

| Файл | Откуда взять |
|---|---|
| `Assembly-CSharp.dll` | `<игра>\Risk of Rain 2_Data\Managed\` |
| `UnityEngine.dll` | `<игра>\Risk of Rain 2_Data\Managed\` — legacy-сборка, в ней определён MonoBehaviour; без неё ошибка «Тип "MonoBehaviour" определен в сборке, на которую нет ссылки» |
| `UnityEngine.CoreModule.dll` | `<игра>\Risk of Rain 2_Data\Managed\` |
| `UnityEngine.AnimationModule.dll` | `<игра>\Risk of Rain 2_Data\Managed\` |
| `UnityEngine.PhysicsModule.dll` | `<игра>\Risk of Rain 2_Data\Managed\` |
| `UnityEngine.AssetBundleModule.dll` | `<игра>\Risk of Rain 2_Data\Managed\` |
| `BepInEx.dll`, `0Harmony.dll` | `<игра>\BepInEx\core\` |
| `R2API.dll` | `<игра>\BepInEx\plugins\` |

> **Важно:** `UnityEngine.Networking.dll` копировать НЕ нужно — в актуальных билдах
> RoR2 (Unity 2019.4+) такой сборки в папке `Managed\` нет, сетевые типы
> (`NetworkServer`, `NetworkIdentity`) находятся внутри `Assembly-CSharp.dll`.

Быстрая копировалка из PowerShell (поправьте путь к игре при необходимости):

```powershell
$game = "C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2"
$lib  = "RoR2FirstPersonBrawler\RoR2FirstPersonBrawler\lib"
New-Item -ItemType Directory -Force $lib | Out-Null
Copy-Item "$game\Risk of Rain 2_Data\Managed\Assembly-CSharp.dll" $lib
# ВАЖНО: UnityEngine.dll (legacy-сборка, Version=0.0.0.0) обязателен — в нём
# определён MonoBehaviour, от которого наследуются классы Assembly-CSharp.
# Без него будет ошибка «Тип "MonoBehaviour" определен в сборке, на которую нет ссылки».
Copy-Item "$game\Risk of Rain 2_Data\Managed\UnityEngine.dll" $lib
Copy-Item "$game\Risk of Rain 2_Data\Managed\UnityEngine.*Module.dll" $lib
Copy-Item "$game\BepInEx\core\BepInEx.dll","$game\BepInEx\core\0Harmony.dll" $lib
if (Test-Path "$game\BepInEx\plugins\R2API.dll") { Copy-Item "$game\BepInEx\plugins\R2API.dll" $lib }
```

После копирования: закройте VS → удалите `bin\`, `obj\`, `.vs\` → откройте sln → Restore → Rebuild.
Папка `lib\` в `.gitignore` (DLL — проприетарные файлы Steam, их нельзя коммитить).

### Ошибки «не найден тип или имя пространства имен RoR2 / UnityEngine / BepInEx»

Эти ошибки (CS0246) означают, что компилятор не увидел DLL игры — то есть путь `RoR2Path` не задан или указан неверно, либо в игре не установлен BepInEx. Начиная с этой версии проект сам определяет путь к Steam-установке (через реестр), а при проблемах выводит в Output одно явное сообщение `[RoR2FirstPersonBrawler] ВНИМАНИЕ: не найдено ...` со списком недостающих DLL и текущим `RoR2Path`. Чек-лист:

1. **Синхронизируйте проект** (`git pull`) — исправления csproj применяются только после обновления кода на вашей машине.
2. Проверьте, что файлы реально существуют:
   - `<игра>\Risk of Rain 2_Data\Managed\Assembly-CSharp.dll`
   - `<игра>\Risk of Rain 2_Data\Managed\UnityEngine.dll` (legacy-сборка с MonoBehaviour)
   - `<игра>\Risk of Rain 2_Data\Managed\UnityEngine.CoreModule.dll`
   - `<игра>\BepInEx\core\BepInEx.dll` и `<игра>\BepInEx\core\0Harmony.dll`
   - `<игра>\BepInEx\plugins\R2API.dll` (если мод использует R2API)
3. Задайте путь к игре (см. п. 2 выше) и перезапустите VS после изменения переменной окружения (VS наследует окружение при запуске). Если игра установлена на другом диске/в другой библиотеке Steam — задайте путь явно через `csproj.user`, автоопределение покрывает только основную Steam-папку.
4. **Обязательно очистите кэш**: закройте решение → удалите папки `bin\`, `obj\` и скрытую `.vs\` в корне решения → откройте sln заново → Restore → Rebuild. Без удаления `obj\.vs` IntelliSense может продолжать показывать старые CS0246 даже при правильном пути.
5. Прогоните диагностику из командной строки, чтобы увидеть точный разрешённый путь:
   ```powershell
   msbuild RoR2FirstPersonBrawler\RoR2FirstPersonBrawler\RoR2FirstPersonBrawler.csproj /t:DiagnoseGameReferences /p:DiagnoseOnly=true
   ```
   Сообщение об ошибке покажет значение `RoR2Path` и список отсутствующих файлов.
6. Если IntelliSense всё ещё подчёркивает типы, но сборка проходит — Language Service не перечитал проект: Solution → Reload Project (или перезапуск VS).
7. Убедитесь, что это **Risk of Rain 2** (не Return of Obelisk и не другая игра): пути к Managed-сборкам специфичны для RoR2.

### Ошибка «Тип "MonoBehaviour" определен в сборке, на которую нет ссылки. Добавьте ссылку на UnityEngine, Version=0.0.0.0»

Причина: подключены только модульные сборки (`UnityEngine.CoreModule.dll` и т.д.), но отсутствует legacy-сборка `UnityEngine.dll` (Version=0.0.0.0). В неё ссылается таблица типов Assembly-CSharp.dll (базовые классы вроде `CharacterBase : MonoBehaviour`), и компилятор требует именно её, а не CoreModule. Решение:
1. Скопируйте `<игра>\Risk of Rain 2_Data\Managed\UnityEngine.dll` в папку `lib\` (рекомендуется) — проект подхватит её автоматически. Эта сборка есть в каждой установке RoR2.
2. Если используете режим пути к игре — убедитесь, что `UnityEngine.dll` присутствует по пути `RoR2Path` (csproj теперь ссылается на него явно).
3. Если DLL физически недоступна, при сборке будет сгенерирована временная facade-сборка `UnityEngine.dll` в `lib\` (позволяет скомпилировать, но для запуска в игре замените её на настоящую).
4. После изменений: закройте VS → удалите `bin\`, `obj\`, `.vs\` → Rebuild.

### Ошибки «тип BuffDef/HealthComponent не найден в RoR2» + «NetworkServer не существует» после подключения lib\

Причина почти всегда одна: в `lib\` попала **синтетическая facade-сборка** `UnityEngine.dll`
(крошечный файл, единицы КБ), сгенерированная предыдущей версией проекта вместо настоящей
сборки игры. Компилятор использует её как базовые типы, и все классы Assembly-CSharp,
наследующие от MonoBehaviour/Component, становятся «не найдены». Что сделано в проекте:
1. Target `ValidateLibReferences` перед сборкой проверяет размеры ключевых DLL в `lib\`;
   facade-заглушки (< 100 КБ) автоматически удаляются, а сборка останавливается с понятным
   текстом ошибки вместо потока CS0246.
2. Фикс у вас: `git pull` → удалите из `lib\` файл `UnityEngine.dll` размером меньше ~100 КБ →
   скопируйте **настоящие** DLL из установки игры (скрипт копирования — выше в README) →
   удалите `bin\`, `obj\`, `.vs\` → Rebuild.
3. «NetworkServer не существует»: тип находится в `Assembly-CSharp.dll` namespace
   `UnityEngine.Networking` (отдельной сборки UnityEngine.Networking в современных билдах нет).
   Проверьте, что `Assembly-CSharp.dll` в `lib\` настоящий (размер — несколько МБ) и не
   заблокирован Windows (ПКМ → Свойства → «Разблокировать»).

### Что изменилось при переносе с VS2022
| Было (VS2022, legacy) | Стало (VS2026) |
|---|---|
| Legacy `.csproj` с явным списком `<Compile>` | SDK-style `.csproj`, файлы включаются автоматически |
| `TargetFrameworkVersion v4.7.2` + `LangVersion 7.3` | `TargetFramework net35` (требование Mono-рантайма RoR2) + `LangVersion 12` |
| `PostBuildEvent` с `xcopy` (ломал сборку без заданного пути) | MSBuild-target `DeployToBepInEx` с условием `Exists(...)` |
| Импорт `Microsoft.Common.props` / `Microsoft.CSharp.targets` | Не нужен в SDK-проектах |
| Решение помечено `Visual Studio Version 17` | Пересохранено под `Visual Studio Version 18` (VS2026) |

Код C# не изменялся — все исходники совместимы как со старым, так и с новым форматом проекта.
