# RoR2Spec
RoR2 Specter Mod Spec

## Сборка (Visual Studio 2026 Insiders)

Проект адаптирован под VS2026: используется SDK-style `.csproj` (TargetFramework `net48`, LangVersion `12`).

1. Установите в VS2026 workload **.NET desktop development** (.NET Framework 4.8 targeting pack; reference-асsemblies также подтягиваются NuGet-пакетом `Microsoft.NETFramework.ReferenceAssemblies` при restore, ставить что-либо вручную не нужно).
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
3. Генерация автоматических facade-заглушек из проекта **полностью удалена** — поддельные сборки только портили ссылки. `UnityEngine.dll` (~100+ КБ) обязан быть настоящим файлом из `<игра>\Risk of Rain 2_Data\Managed\`; скопируйте его в `lib\` или задайте верный путь к игре.
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

**Вторая частая причина тех же ошибок: в `lib\` НЕТ `UnityEngine.CoreModule.dll`.**
Даже если там лежат настоящие `Assembly-CSharp.dll` и `UnityEngine.dll`, без CoreModule
ошибки «BuffDef/HealthComponent не найден в RoR2» останутся: в Mono-сборках Unity базовые
классы `Component`/`MonoBehaviour`/`GameObject` объявлены именно в
`UnityEngine.CoreModule.dll`, а legacy `UnityEngine.dll` содержит только перенаправления
(type-forwards) на неё. Без CoreModule компилятор не может связать классы Assembly-CSharp
с их базовыми типами и отбраковывает все ссылки на них. Поэтому в `lib\` обязательны
**все** модули Unity из скрипта выше (`UnityEngine.*Module.dll`) — скопируйте их целиком.
Проект теперь проверяет это явно: target `RequireCoreModuleInLib` останавливает сборку с
понятным текстом ошибки, если в `lib\` есть DLL, но отсутствует `UnityEngine.CoreModule.dll`.

### Массовые CS0246 («тип BuffDef/HealthComponent не найден в RoR2», «NetworkServer не существует») при DLL в lib\ размером ~100–140 КБ

Самая частая причина на сегодняшний день: **DLL скопированы из zip-архива репозитория GitHub**
(`Code → Download ZIP` → `RoR2Spec-main.zip`). В коммитах проекта файлы `lib\Assembly-CSharp.dll`,
`UnityEngine.dll`, `BepInEx.dll` — это заглушки ~130 КБ (настоящая `Assembly-CSharp.dll` весит
несколько МЕГАБАЙТ, `UnityEngine.CoreModule.dll` — больше мегабайта). Проект считал наличие
любого файла в `lib\` сигналом автономного режима и ссылался именно на заглушки, поэтому все
типы RoR2 оставались «не найдены», даже когда путь к игре определялся корректно.

Что сделано в проекте (начиная с этого коммита):
1. Target `PurgeBrokenLibRefs`: перед сборкой автоматически **удаляет** из `lib\` все DLL
   меньше 500 КБ (заглушки) и пишет в лог, какие файлы удалены. Сборка после этого использует
   ссылки по пути к игре (режим B).
2. Ссылки выбираются по факту существования конкретного файла (`Exists()` на элементе), а не по
   общему флагу режима — битый файл больше не может «перехватить» ссылку.
3. Исправлено автоопределение пути: ранее производные пути (`...\Risk of Rain 2_Data\Managed`)
   вычислялись ДО автоопределения из реестра Steam и оставались указывать на дефолтный
   `C:\Program Files (x86)\Steam\...`. Теперь есть target `RecomputeGamePaths`, который
   пересчитывает их после определения реального пути (например, `F:\Steam`).

Фикс у вас:
1. `git pull` (получить этот коммит).
2. Rebuild — проект сам удалит заглушки из `lib\`; в логе появится строка
   «из lib\ удалены слишком маленькие (ненастоящие) сборки: …».
3. Если хотите полностью автономный режим A: скопируйте настоящие DLL из установленной игры
   (скрипт копирования выше) — файлы меньше 500 КБ проект сочтёт подделками и проигнорирует.
4. Проверьте размеры: `Assembly-CSharp.dll` — единицы МБ; если он весит ~137 КБ — это заглушка
   из архива GitHub, а не из игры.

### MSB3268 «не удалось разрешить основную ссылку Assembly-CSharp.dll ... netstandard 2.1» + массовые CS0246 при корректных DLL в lib\

Признак: все DLL в `lib\` настоящие (Assembly-CSharp ~много МБ, CoreModule ~1+ МБ),
но сборка всё равно падает с предупреждениями MSB3268 и потоком CS0246 по всем типам
RoR2/UnityEngine/BepInEx. В логе сборки видны строки вида
`C:\Program Files\dotnet\sdk\...\Microsoft.NET.RuntimeIdentifierInference.targets` —
это значит, что проект собирается **Roslyn-компилятором из dotnet SDK**, который не
поддерживает целевой фреймворк net48 и не умеет разрешать ссылки на сборки игры,
косвенно зависящие от netstandard. Что сделано в проекте:
1. В корне репозитория добавлен `Directory.Build.props`: отключает Roslyn-компилятор
   SDK (`UsingMicrosoftNETSdkRoslynCompilers=false`) и указывает нативный
   `csc.exe` из .NET Framework (`%WINDIR%\Microsoft.NET\Framework\v4.0.30319`) —
   единственный компилятор, корректно работающий со старыми TFM.
2. Фикс у вас: `git pull` → закройте VS → удалите `bin\`, `obj\`, `.vs\` → откройте sln →
   Rebuild. Предупреждение MSB3277 (конфликт версий косвенных зависимостей) подавлено —
   оно безвредно: во время рантайма все сборки подгружает BepInEx/Mono.

### Что изменилось при переносе с VS2022
| Было (VS2022, legacy) | Стало (VS2026) |
|---|---|
| Legacy `.csproj` с явным списком `<Compile>` | SDK-style `.csproj`, файлы включаются автоматически |
| `TargetFrameworkVersion v4.7.2` + `LangVersion 7.3` | `TargetFramework net48` (.NET Framework 4.8) + `LangVersion 12` |
| `PostBuildEvent` с `xcopy` (ломал сборку без заданного пути) | MSBuild-target `DeployToBepInEx` с условием `Exists(...)` |
| Импорт `Microsoft.Common.props` / `Microsoft.CSharp.targets` | Не нужен в SDK-проектах |
| Решение помечено `Visual Studio Version 17` | Пересохранено под `Visual Studio Version 18` (VS2026) |

Код C# не изменялся — все исходники совместимы как со старым, так и с новым форматом проекта.

### CS2007 «Нераспознанный параметр: /sdkpath:»

Причина: предыдущая версия проекта принудительно подменяла компилятор на нативный
csc.exe из .NET Framework v4.0 — MSBuild передаёт ему современные ключи SDK-проектов,
которые тот не понимает. Исправлено: компилятор больше не подменяется (настройки в
`Directory.Build.props`). Обновите репозиторий (`git pull`), удалите `bin\`, `obj\`, `.vs\`,
пересоберите.

### MSB3268 «netstandard 2.1 нельзя разрешить для .NETFramework v3.5» + CS0246

Это предупреждение — особенность проверки ссылок в dotnet SDK; игровые DLL
(Assembly-CSharp, R2API) косвенно ссылаются на netstandard shim-сборки, что нормально
для Mono-рантайма RoR2 и НЕ мешает ни компиляции, ни работе плагина. В проекте оно
подавлено (`NoWarn MSB3268` + обход apt-фильтрации в `Directory.Build.props`).
Если после `git pull` предупреждения остались — значит не обновлённый csproj: проверьте
коммит через `git log -1`.

### Steam установлен на другом диске (например, F:\Steam): путь к игре определялся, но ссылки шли на C:\Program Files (x86)

Симптом: в логе сборки виден `InstallPath REG_SZ F:\Steam`, но следом —
`Managed-путь для ссылок: C:\Program Files (x86)\Steam\...` и массовые CS0246.

Причина (исправлена): элементы `<Reference>` с путями игры создаются при **загрузке**
проекта — до выполнения любых target'ов автоопределения. Автоопределение меняло только
свойство пути, но не уже созданные ссылки. Теперь target `RecomputeGamePaths` после
автоопределения пересоздаёт все ссылки режима B заново (Remove + Include) с реальным
путём; поиск игры вынесен в скрипт `FindRoR2Path.ps1` (реестр HKLM/HKCU + все
библиотеки Steam из `libraryfolders.vdf`).

Что сделать: `git pull` → закрыть VS → удалить `bin\`, `obj\`, `.vs\` → открыть sln → Rebuild.
В логе должна появиться строка `[RoR2FirstPersonBrawler] Путь к игре определён автоматически: F:\Steam\steamapps\common\Risk of Rain 2`.
Если игра не найдена автоматически — задайте путь явно в `RoR2FirstPersonBrawler.csproj.user`:
```xml
<Project>
  <PropertyGroup>
    <RoR2Path>F:\Steam\steamapps\common\Risk of Rain 2</RoR2Path>
  </PropertyGroup>
</Project>
```
