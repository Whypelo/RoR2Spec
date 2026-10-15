using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Camera
{
    /// <summary>
    /// Конфигурация вида от первого лица: пути костей, значения near clip и FOV
    /// по сурвайворам (спецификация 6.1). Аналогичен подходу мода True First Person.
    /// </summary>
    public static class CameraConfig
    {
        /// <summary>Путь к кости головы Specter по умолчанию (спецификация 4.2).</summary>
        public const string DefaultHeadBonePath =
            "Armature/base/spine.001/spine.002/spine.003/spine.004/neck/head";

        /// <summary>Явные override-пути костей для конкретных тел.</summary>
        public static readonly Dictionary<string, string> survivorDefaultBoneOverride =
            new Dictionary<string, string>
            {
                { "SpecterBody", DefaultHeadBonePath }
            };

        /// <summary>Персонализированные near-clip значения по умолчанию.</summary>
        public static readonly Dictionary<string, float> survivorClipDefaultValues =
            new Dictionary<string, float>
            {
                { "SpecterBody", 0.15f }
            };

        /// <summary>Базовый FOV (используется если конфиг-значение недоступно).</summary>
        public const float BaseFov = 90f;

        /// <summary>FOV-буст во время Rift Strike (спецификация 6.3).</summary>
        public const float DashFovBoost = 10f;

        /// <summary>Смещение камеры вперёд при ударе Primary («выпад»).</summary>
        public const float AttackLungeDistance = 0.15f;

        /// <summary>Имена костей-кандидатов при поиске через ChildLocator.</summary>
        public static readonly string[] BoneSearchPriority = { "head", "chest" };

        /// <summary>Возвращает bone path для тела (override или поиск по имени).</summary>
        public static string GetBonePath(string bodyName)
        {
            if (string.IsNullOrEmpty(bodyName)) return DefaultHeadBonePath;
            return survivorDefaultBoneOverride.TryGetValue(bodyName, out var path)
                ? path
                : DefaultHeadBonePath;
        }

        /// <summary>Near clip для тела: персональное значение или глобальный конфиг.</summary>
        public static float GetClipValue(string bodyName)
        {
            if (!string.IsNullOrEmpty(bodyName) &&
                survivorClipDefaultValues.TryGetValue(bodyName, out var clip))
            {
                return clip;
            }
            return FirstPersonBrawlerPlugin.FP_ClipValue.Value;
        }
    }
}
