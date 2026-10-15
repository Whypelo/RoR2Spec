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

### Ошибки «не найден тип или имя пространства имен RoR2 / UnityEngine / BepInEx»

Эти ошибки (CS0246) означают, что компилятор не увидел DLL игры — то есть путь `RoR2Path` не задан или указан неверно, либо в игре не установлен BepInEx. Чек-лист:

1. Проверьте, что файлы реально существуют:
   - `<игра>\Risk of Rain 2_Data\Managed\Assembly-CSharp.dll`
   - `<игра>\Risk of Rain 2_Data\Managed\UnityEngine.CoreModule.dll`
   - `<игра>\BepInEx\core\BepInEx.dll` и `<игра>\BepInEx\core\0Harmony.dll`
   - `<игра>\BepInEx\plugins\R2API.dll` (если мод использует R2API)
2. Задайте путь к игре (см. п. 2 выше) и перезапустите VS после изменения переменной окружения (VS наследует окружение при запуске).
3. В Output окна сборки должно появиться явное предупреждение `[RoR2FirstPersonBrawler] Не найдена игра по пути: ...` — если оно есть, путь точно неверный; если его нет, а ошибки остаются — сделайте **Build → Clean Solution**, удалите папки `bin\` и `obj\`, затем Restore → Rebuild.
4. Если IntelliSense всё ещё подчёркивает типы, но сборка проходит — закройте решение и удалите скрытую папку `.vs\` в корне (кэш Roslyn Language Service), затем откройте заново.
5. Убедитесь, что это **Risk of Rain 2** (не Return of Obelisk и не другая игра): пути к Managed-сборкам специфичны для RoR2.

### Что изменилось при переносе с VS2022
| Было (VS2022, legacy) | Стало (VS2026) |
|---|---|
| Legacy `.csproj` с явным списком `<Compile>` | SDK-style `.csproj`, файлы включаются автоматически |
| `TargetFrameworkVersion v4.7.2` + `LangVersion 7.3` | `TargetFramework net35` (требование Mono-рантайма RoR2) + `LangVersion 12` |
| `PostBuildEvent` с `xcopy` (ломал сборку без заданного пути) | MSBuild-target `DeployToBepInEx` с условием `Exists(...)` |
| Импорт `Microsoft.Common.props` / `Microsoft.CSharp.targets` | Не нужен в SDK-проектах |
| Решение помечено `Visual Studio Version 17` | Пересохранено под `Visual Studio Version 18` (VS2026) |

Код C# не изменялся — все исходники совместимы как со старым, так и с новым форматом проекта.
