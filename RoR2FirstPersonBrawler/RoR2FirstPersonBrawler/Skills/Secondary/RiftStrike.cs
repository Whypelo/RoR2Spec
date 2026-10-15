using RoR2;
using RoR2.SkillSlots;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Skills.Secondary
{
    /// <summary>
    /// Secondary — Rift Strike (спецификация 5.2).
    /// Рывок вперёд на 8 м сквозь врагов, 300% урона каждому задетому противнику.
    /// При попадании хотя бы в одного врага кулдаун сокращается на 1.5 сек.
    /// </summary>
    public static class RiftStrike
    {
        public const float BaseCooldown = 5f;
        public const float CooldownReductionOnHit = 1.5f;
        public const float DashDistance = 8f;
        public const float DashDuration = 0.25f;   // ~32 м/с пиковая скорость
        public const float DamagePercent = 3.0f;
        public const float HitRadius = 1.5f;

        public static SkillDef CreateSkillDef()
        {
            return new SkillDef
            {
                icon = new IconTexture(Survivors.Icons.RiftStrike, 64, 64),
                baseCooldown = BaseCooldown,
                cooldownScale = 1f,
                interruptStatic = true,
                nameToken = Survivors.ContentPack.SkillToken("RIFTSTRIKE"),
                descriptionToken = Survivors.ContentPack.SkillToken("RIFTSTRIKE"),
                activationState = new EntityStateData
                {
                    runChildren = false,
                    duration = DashDuration + 0.15f, // рывок + восстановление
                    enterAnimationIndex = 4,
                    exitAnimationIndex = 5,
                    animationName = "RiftStrike",
                    customName = "RiftStrikeDash"
                },
                deactivationBehavior = SkillDef.DeactivateBehavior.RestartOnFalse,
                activationBehaviors = new SkillActivation[] { new SkillActivation(typeof(EntityStates.RiftStrikeState)) }
            };
        }

        /// <summary>
        /// Тик рывка: наносит урон всем врагам в сфере радиуса вокруг текущей
        /// позиции через стандартный OverlapAttack-конвейер. Возвращает true,
        /// если был хотя бы один hit (для сокращения кулдауна).
        /// </summary>
        public static bool DashTick(GameObject character, Vector3 position)
        {
            var body = character ? character.GetComponent<CharacterBody>() : null;
            if (!body) return false;

            bool anyHit = false;

            var attack = new OverlapAttack
            {
                attacker = character,
                damageType = DamageType.Generic,
                hitBoxGroup = Survivors.ContentPack.slashHitBoxGroup,
                procCoefficient = 1f,
                damage = body.damage * DamagePercent,
                falloffModel = OverlapAttack.FalloffModel.None
            };
            attack.onHitEnemy += (ref AttackReport report) =>
            {
                if (report.result == AttackResult.Attacked) anyHit = true;
                return true;
            };
            attack.ProcessHits();

            // Камера: FOV-буст рывка (спецификация 6.3).
            Camera.FirstPersonCameraController.NotifyDash(character);

            return anyHit;
        }

        /// <summary>Сокращение кулдауна при попадании (вызывается из состояния рывка).</summary>
        public static void ApplyCooldownReduction(GameObject character)
        {
            // CharacterMaster владеет слотами; сокращаем кулдаун secondary на сервере.
            var master = character ? character.GetComponent<CharacterMaster>() : null;
            if (!master)
            {
                var body = character ? character.GetComponent<CharacterBody>() : null;
                if (body && body.master) master = body.master;
            }
            if (!master || !UnityEngine.Networking.NetworkServer.active) return;

            var slotMaster = master.GetComponents<CharacterSkillSlotMaster>()[1]; // secondary
            slotMaster.AddCooldown(-CooldownReductionOnHit);
        }

        /// <summary>
        /// Расчёт целевой точки рывка: 8 м вперёд по направлению взгляда/движения.
        /// Используется состоянием EntityStates для CharacterMotor.Motor.
        /// </summary>
        public static Vector3 ComputeDashTarget(Transform transform)
        {
            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            return transform.position + forward.normalized * DashDistance;
        }
    }
}
