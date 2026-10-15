using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Skills.Primary
{
    /// <summary>
    /// Primary — Phase Slash (спецификация 5.1).
    /// Комбо из трёх ударов фазовым клинком: 130% / 130% / 250% (AoE + Phase Mark).
    /// Реализация через OverlapAttack с hitBoxGroup "Slash", procCoefficient = 1.0.
    /// </summary>
    public static class PhaseSlash
    {
        // Параметры комбо (таблица 5.1)
        public const float Stage1Damage = 1.30f;   // % базового урона
        public const float Stage2Damage = 1.30f;
        public const float Stage3Damage = 2.50f;
        public const float Stage1Radius = 2.5f;
        public const float Stage2Radius = 2.5f;
        public const float Stage3Radius = 3.5f;    // AoE-волна третьего удара
        public const float ComboResetDelay = 1.2f; // сброс комбо без ударов
        public const float PhaseMarkDuration = 3f;
        public const float EmpoweredMultiplier = 2f; // после Phase Shift (5.3)

        public static SkillDef CreateSkillDef()
        {
            var skilldef = new SkillDef
            {
                icon = new IconTexture(Survivors.Icons.PhaseSlash, 64, 64),
                baseCooldown = 0.4f,         // темп комбо между ударами
                cooldownScale = 1f,          // учитывает стат Cooldown Scaling
                interruptStatic = false,
                activationState = new EntityStateData
                {
                    runChildren = false,
                    duration = 0.3f,
                    enterAnimationIndex = 2,
                    exitAnimationIndex = 3,
                    animationName = "AttackPhaseSlash1",
                    customName = "PhaseSlashStage1"
                },
                nameToken = Survivors.ContentPack.SkillToken("PHASESLASH"),
                descriptionToken = Survivors.ContentPack.SkillToken("PHASESLASH"),
                // Комбо не прерывается спринтом/прыжком — как у Mercenary:
                deactivationBehavior = SkillDef.DeactivateBehavior.RestartOnFalse,
                activationBehaviors = new SkillActivation[] { new SkillActivation(typeof(EntityStates.PhaseSlashStage1)) }
            };

            return skilldef;
        }

        /// <summary>
        /// Хелпер нанесения урона — вызывается из EntityStates SpecterComboStates
        /// на кадрах анимации (animation events или фиксированном тайминге состояния).
        /// Использует стандартный конвейер RoR2: OverlapAttack.ProcessHits.
        /// </summary>
        public static void DoSlash(GameObject attacker, float damagePercent,
            float radius, bool aoeWave, bool empowered)
        {
            var body = attacker ? attacker.GetComponent<CharacterBody>() : null;
            if (!body || !Survivors.ContentPack.slashHitBoxGroup) return;

            var attack = new OverlapAttack
            {
                attacker = attacker,
                damageType = DamageType.Generic,
                hitBoxGroup = Survivors.ContentPack.slashHitBoxGroup,
                procCoefficient = 1f,
                damage = body.damage * damagePercent * (empowered ? EmpoweredMultiplier : 1f),
                falloffModel = OverlapAttack.FalloffModel.None
            };

            if (aoeWave)
            {
                // AoE-волна третьего удара + гарантированная Phase Mark (спецификация 5.1).
                attack.onHitEnemy += (ref AttackReport report) =>
                {
                    if (report.result == AttackResult.Attacked)
                    {
                        Effects.ApplyBuff(attacker, report.victim,
                            Survivors.ContentPack.phaseMarkBuff, PhaseMarkDuration);
                    }
                    return true;
                };
            }

            attack.ProcessHits();

            // Камера: «выпад» при атаке Primary (спецификация 6.3).
            Camera.FirstPersonCameraController.NotifyAttack(attacker);
        }
    }
}
