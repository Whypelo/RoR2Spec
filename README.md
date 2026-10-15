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

### Что изменилось при переносе с VS2022
| Было (VS2022, legacy) | Стало (VS2026) |
|---|---|
| Legacy `.csproj` с явным списком `<Compile>` | SDK-style `.csproj`, файлы включаются автоматически |
| `TargetFrameworkVersion v4.7.2` + `LangVersion 7.3` | `TargetFramework net35` (требование Mono-рантайма RoR2) + `LangVersion 12` |
| `PostBuildEvent` с `xcopy` (ломал сборку без заданного пути) | MSBuild-target `DeployToBepInEx` с условием `Exists(...)` |
| Импорт `Microsoft.Common.props` / `Microsoft.CSharp.targets` | Не нужен в SDK-проектах |
| Решение помечено `Visual Studio Version 17` | Пересохранено под `Visual Studio Version 18` (VS2026) |

Код C# не изменялся — все исходники совместимы как со старым, так и с новым форматом проекта.
