using RoR2;
using RoR2.SkillSlots;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Skills.Utility
{
    /// <summary>
    /// Utility — Phase Shift (спецификация 5.3).
    /// 2.5 сек неуязвимости и полупрозрачности, +30% к скорости, следующая
    /// Primary-атака наносит двойной урон и гарантированно накладывает Phase Mark.
    /// </summary>
    public static class PhaseShift
    {
        public const float Cooldown = 10f;
        public const float Duration = 2.5f;
        public const float MoveSpeedBonus = 1.3f; // +30%

        private static GameObject _transparencyVfxPrefab; // шейдер с прозрачностью (этап 3/5)

        public static SkillDef CreateSkillDef()
        {
            return new SkillDef
            {
                icon = new IconTexture(Survivors.Icons.PhaseShift, 64, 64),
                baseCooldown = Cooldown,
                cooldownScale = 1f,
                interruptStatic = false,
                nameToken = Survivors.ContentPack.SkillToken("PHASESHIFT"),
                descriptionToken = Survivors.ContentPack.SkillToken("PHASESHIFT"),
                activationState = new EntityStateData
                {
                    runChildren = false,
                    duration = Duration,
                    enterAnimationIndex = 6,
                    exitAnimationIndex = 7,
                    animationName = "PhaseShift",
                    customName = "PhaseShiftActive"
                },
                deactivationBehavior = SkillDef.DeactivateBehavior.Ignore,
                activationBehaviors = new SkillActivation[] { new SkillActivation(typeof(EntityStates.PhaseShiftState)) }
            };
        }

        /// <summary>Начало фазы: неуязвимость, бафф, усиление следующей атаки.</summary>
        public static void Begin(GameObject character)
        {
            var body = character ? character.GetComponent<Survivors.SpecterBody>() : null;
            if (!body) return;

            // 1. Визуальный бафф-статус (иконка над головой / HUD).
            if (UnityEngine.Networking.NetworkServer.active && Survivors.ContentPack.phaseShiftBuff)
            {
                var hc = character.GetComponent<HealthComponent>();
                if (hc) hc.AddBuff(Survivors.ContentPack.phaseShiftBuff, Time.time + Duration);
            }

            // 2. Неуязвимость: RoR2-паттерн "hidden invincibility" как у Hunter's
            //    HideInShadows — ставим бафф NotOnMap + i-frame через GodMode.
            //    Для простоты этапа 1-2 используем встроенный GodModeComponent мастера.
            var godComponent = character.GetComponent<RoR2.GodModeBehavior>();
            if (godComponent) godComponent.enabled = true;

            // 3. Бонус скорости передвижения (CharacterBody.moveSpeed реплицируется).
            body.moveSpeed *= MoveSpeedBonus;

            // 4. Флаг усиления следующего Primary ( consumed в PhaseSlashCombo ).
            body.NextAttackEmpowered = true;

            // 5. Камера: эффекты размытия/хроматической аберрации (спецификация 6.3).
            Camera.FirstPersonCameraController.NotifyPhaseShift(character, active: true);
        }

        /// <summary>Конец фазы: снятие всех модификаторов.</summary>
        public static void End(GameObject character)
        {
            var body = character ? character.GetComponent<Survivors.SpecterBody>() : null;
            if (!body) return;

            var godComponent = character.GetComponent<RoR2.GodModeBehavior>();
            if (godComponent) godComponent.enabled = false;

            body.moveSpeed /= MoveSpeedBonus;

            Camera.FirstPersonCameraController.NotifyPhaseShift(character, active: false);
        }

        /// <summary>Регистрация VFX-префаба прозрачности (этап 3, после импорта модели).</summary>
        public static void RegisterTransparencyVfx(GameObject prefab)
        {
            _transparencyVfxPrefab = prefab;
        }

        public static GameObject TransparencyVfx
        {
            get { return _transparencyVfxPrefab; }
        }
    }
}
