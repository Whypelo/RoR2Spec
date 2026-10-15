using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Skills.Special
{
    /// <summary>
    /// Special — Void Eruption (спецификация 5.4).
    /// Заряд 1 сек, затем AoE-волна радиусом 10 м: 800% урона всем врагам.
    /// Враги с дебаффом Phase Mark получают +400% урона (итого 1200%) и оглушение на 2 сек.
    /// </summary>
    public static class VoidEruption
    {
        public const float Cooldown = 12f;
        public const float ChargeTime = 1f;
        public const float Radius = 10f;
        public const float DamagePercent = 8.0f;
        public const float MarkedBonusPercent = 4.0f; // доп. урон по Phase Mark
        public const float MarkedStunDuration = 2f;

        public static SkillDef CreateSkillDef()
        {
            return new SkillDef
            {
                icon = new IconTexture(Survivors.Icons.VoidEruption, 64, 64),
                baseCooldown = Cooldown,
                cooldownScale = 1f,
                interruptStatic = true,
                nameToken = Survivors.ContentPack.SkillToken("VOIDERUPTION"),
                descriptionToken = Survivors.ContentPack.SkillToken("VOIDERUPTION"),
                activationState = new EntityStateData
                {
                    runChildren = false,
                    duration = ChargeTime + 0.4f, // заряд + вспышка
                    enterAnimationIndex = 8,
                    exitAnimationIndex = 9,
                    animationName = "VoidEruptionWindup",
                    customName = "VoidEruptionCharge"
                },
                deactivationBehavior = SkillDef.DeactivateBehavior.RestartOnFalse,
                activationBehaviors = new SkillActivation[] { new SkillActivation(typeof(EntityStates.VoidEruptionState)) }
            };
        }

        /// <summary>
        /// Высвобождение волны в конце зарядки. Вызывается из OnExit/таймера
        /// состояния заряда. BlastAttack по спецификации 5.4: radius 10, procCoef 1.0.
        /// Враги с Phase Mark получают +400% урона и стан 2 сек.
        /// </summary>
        public static void Fire(GameObject character)
        {
            var body = character ? character.GetComponent<CharacterBody>() : null;
            if (!body) return;

            var origin = character.transform.position + Vector3.up * 1f;

            var blast = new BlastAttack
            {
                attacker = character,
                inflictor = character,
                position = origin,
                radius = Radius,
                baseDamage = body.damage * DamagePercent,
                baseForce = 0f,
                damageType = DamageType.Generic,
                procCoefficient = 1f,
                falloffModel = BlastAttack.FalloffModel.None
            };

            blast.onHitServerCallback = (ref BlastAttack.ResultInfo result) =>
            {
                var phaseMark = Survivors.ContentPack.phaseMarkBuff;
                var victimBody = result.target ? result.target.GetComponent<CharacterBody>() : null;
                if (phaseMark && victimBody && victimBody.HasBuff(phaseMark))
                {
                    // Бонус по отмеченным врагам: +400% урона и стан 2 сек (5.4).
                    result.damage += body.damage * MarkedBonusPercent;
                    StunController.ApplyStun(victimBody, character,
                        Time.time + MarkedStunDuration, Vector3.zero);
                }
            };

            blast.Fire();

            // VFX расширяющегося кольца — этап 5 подключит префаб из бандла.
            var vfxPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/EnergyDrainEffect");
            if (vfxPrefab)
            {
                Effects.SpawnEffect(vfxPrefab, origin, Quaternion.identity);
            }
        }
    }
}
