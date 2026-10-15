using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler
{
    /// <summary>
    /// Небольшие переиспользуемые хелперы эффектов/баффов.
    /// Все сетевые действия выполняются только на сервере (RoR2 authoritative model).
    /// </summary>
    internal static class Effects
    {
        /// <summary>Навесить бафф на цель с длительностью (серверная сторона).</summary>
        public static void ApplyBuff(GameObject attackerGo, GameObject targetGo,
            BuffDef buffDef, float duration)
        {
            if (!attackerGo || !targetGo || !buffDef) return;

            var attackerHC = attackerGo.GetComponent<HealthComponent>();
            var targetHC = targetGo.GetComponent<HealthComponent>();
            if (!attackerHC || !targetHC) return;

            // На клиенте баффы применяются репликацией HealthComponent.AddBuff.
            // Полное имя: отдельная сборка UnityEngine.Networking в современных билдах
            // RoR2 отсутствует, тип NetworkServer находится в Assembly-CSharp.dll.
            if (!UnityEngine.Networking.NetworkServer.active) return;

            targetHC.AddBuff(buffDef, Time.time + duration);
        }

        /// <summary>Спавн простого VFX-заглушителя; этап 5 подключит префабы из бандла.</summary>
        public static void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefab) return;
            if (position == Vector3.zero && rotation == Quaternion.identity)
            {
                Object.Instantiate(prefab);
            }
            else
            {
                Object.Instantiate(prefab, position, rotation);
            }
        }
    }
}
