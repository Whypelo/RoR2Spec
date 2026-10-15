using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Skills.Primary
{
    /// <summary>
    /// Логика комбо Phase Slash: счётчик стадий, окно сброса 1.2 сек,
    /// усиление после Phase Shift. Вызывается из состояний EntityStates.
    /// </summary>
    public static class PhaseSlashCombo
    {
        private class ComboTracker : MonoBehaviour
        {
            public int Stage;               // 0..2 — следующий удар
            public float TimeSinceLastHit;  // для окна сброса
            public GameObject LastTarget;   // сброс при смене цели
        }

        /// <summary>Возвращает индекс стадии (0,1,2) для текущего нажатия.</summary>
        public static int AdvanceCombo(GameObject character)
        {
            var tracker = GetOrCreateTracker(character);
            tracker.Stage = Mathf.Clamp(tracker.Stage + 1, 0, 2);
            return tracker.Stage;
        }

        /// <summary>Текущая стадия без продления (для подбора урона/радиуса).</summary>
        public static int PeekStage(GameObject character)
        {
            var tracker = GetOrCreateTracker(character);
            UpdateTimeout(tracker, character);
            return tracker.Stage;
        }

        /// <summary>Получить параметры удара по стадии.</summary>
        public static void GetStageParams(int stage, out float damagePercent,
            out float radius, out bool aoeWave)
        {
            switch (stage)
            {
                case 0:
                    damagePercent = PhaseSlash.Stage1Damage; radius = PhaseSlash.Stage1Radius;
                    aoeWave = false; break;
                case 1:
                    damagePercent = PhaseSlash.Stage2Damage; radius = PhaseSlash.Stage2Radius;
                    aoeWave = false; break;
                default:
                    damagePercent = PhaseSlash.Stage3Damage; radius = PhaseSlash.Stage3Radius;
                    aoeWave = true; break; // 3-й удар — волна + Phase Mark
            }
        }

        /// <summary>Вызывается после реализации удара: сбрасывает усиление Phase Shift.</summary>
        public static void ConsumeEmpowerment(GameObject character)
        {
            var body = character ? character.GetComponent<Survivors.SpecterBody>() : null;
            if (body) body.NextAttackEmpowered = false;
        }

        public static bool IsEmpoweredNext(GameObject character)
        {
            var body = character ? character.GetComponent<Survivors.SpecterBody>() : null;
            return body && body.NextAttackEmpowered;
        }

        private static void UpdateTimeout(ComboTracker tracker, GameObject character)
        {
            // Сброс комбо через 1.2 сек после последнего удара или при смене цели.
            if (Time.time - tracker.TimeSinceLastHit > PhaseSlash.ComboResetDelay)
            {
                tracker.Stage = 0;
            }
        }

        private static ComboTracker GetOrCreateTracker(GameObject character)
        {
            var tracker = character.GetComponent<ComboTracker>();
            if (!tracker)
            {
                tracker = character.AddComponent<ComboTracker>();
            }
            tracker.TimeSinceLastHit = Time.time;
            return tracker;
        }

        /// <summary>Полный сброс (например, при смерти/респауне).</summary>
        public static void ResetCombo(GameObject character)
        {
            var tracker = character ? character.GetComponent<ComboTracker>() : null;
            if (tracker)
            {
                tracker.Stage = -1; // чтобы AdvanceCombo дал 0
                tracker.LastTarget = null;
            }
        }
    }
}
