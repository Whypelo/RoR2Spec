using System.Linq;
using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// Display-префаб Specter: визуальная часть (модель, аниматор, эффекты).
    /// Отделяется от тела согласно архитектуре RoR2 (RoR2.DisplayPrefabController).
    /// До этапа 3 (импорт модели из Blender) использует примитив-заглушку.
    /// </summary>
    public class SpecterDisplay : MonoBehaviour
    {
        [Header("Specter Display")]
        public Renderer headRenderer;     // скрывается в FP-режиме (спецификация 6.2)
        public Renderer bodyRenderer;     // опционально скрывается при HideBody
        public Transform headBone;        // целевая кость камеры (путь из CameraConfig)
        public GameObject bladeVFX;       // клинок остаётся видимым в FP

        private const string HeadBonePath =
            "Armature/base/spine.001/spine.002/spine.003/spine.004/neck/head";

        public void Start()
        {
            // Поиск костей по имени: приоритет head, затем chest (спецификация 6.1).
            if (!headBone)
            {
                headBone = FindTransformRecursive(transform, HeadBonePath.Split('/').Last());
                if (!headBone)
                {
                    headBone = FindTransformRecursive(transform, "chest");
                }
            }
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (!parent || string.IsNullOrEmpty(name)) return null;
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
                var found = FindTransformRecursive(child, name);
                if (found) return found;
            }
            return null;
        }

        /// <summary>Создаёт display-объект-заглушку (этап 1 «болванчик»).</summary>
        public static GameObject CreatePlaceholderDisplay()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "SpecterDisplay";
            var mat = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.25f, 0.7f, 0.9f, 0.85f)
            };
            go.GetComponent<Renderer>().material = mat;

            var display = go.AddComponent<SpecterDisplay>();
            display.bodyRenderer = go.GetComponent<Renderer>();

            Object.DontDestroyOnLoad(go);
            return go;
        }
    }

}
