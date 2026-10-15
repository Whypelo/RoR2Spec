using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// Статический реестр способностей Specter. Вызывается из FirstPersonBrawlerPlugin.Awake()
    /// до регистрации тела (скилл-дефы нужны CharacterSkillSlots).
    /// </summary>
    public static class SpecterSkills
    {
        public static void RegisterSkills()
        {
            // Регистрация типов состояний через SkillAPI (обязательно для сетевой синхронизации).
            SkillAPI.RegisterEntityState(typeof(EntityStates.PhaseSlashStage1));
            SkillAPI.RegisterEntityState(typeof(EntityStates.RiftStrikeState));
            SkillAPI.RegisterEntityState(typeof(EntityStates.PhaseShiftState));
            SkillAPI.RegisterEntityState(typeof(EntityStates.VoidEruptionState));

            // Порядок важен: баффы регистрируем раньше скиллов, т.к. на них ссылаются
            // эффекты и onHit callbacks.
            RegisterBuffs();

            ContentPack.phaseSlash = Skills.Primary.PhaseSlash.CreateSkillDef();
            ContentPack.riftStrike = Skills.Secondary.RiftStrike.CreateSkillDef();
            ContentPack.phaseShift = Skills.Utility.PhaseShift.CreateSkillDef();
            ContentPack.voidEruption = Skills.Special.VoidEruption.CreateSkillDef();

            // Явная регистрация через SkillAPI нужна только для динамической загрузки
            // пресетов; SurvivorAPI подхватит дефы при добавлении тела.
            SkillAPI.Add(ContentPack.phaseSlash);
            SkillAPI.Add(ContentPack.riftStrike);
            SkillAPI.Add(ContentPack.phaseShift);
            SkillAPI.Add(ContentPack.voidEruption);
        }

        private static void RegisterBuffs()
        {
            // Phase Mark: +15% входящего урона на 3 сек (см. спецификацию 5.1/5.4).
            ContentPack.phaseMarkBuff = ScriptableObject.CreateInstance<BuffDef>();
            ContentPack.phaseMarkBuff.name = "SpecterPhaseMark";
            ContentPack.phaseMarkBuff.buffName = "BUFF_SPECTER_PHASEMARK_name";
            ContentPack.phaseMarkBuff.buffDescription = "BUFF_SPECTER_PHASEMARK_desc";
            ContentPack.phaseMarkBuff.debuff = true;
            ContentPack.phaseMarkBuff.canStack = false;
            ContentPack.phaseMarkBuff.displayPercentage = false;
            ContentPack.phaseMarkBuff.hidden = false;

            // Phase Shift: активная неуязвимость + бонус скорости.
            ContentPack.phaseShiftBuff = ScriptableObject.CreateInstance<BuffDef>();
            ContentPack.phaseShiftBuff.name = "SpecterPhaseShift";
            ContentPack.phaseShiftBuff.buffName = "BUFF_SPECTER_PHASESHIFT_name";
            ContentPack.phaseShiftBuff.buffDescription = "BUFF_SPECTER_PHASESHIFT_desc";
            ContentPack.phaseShiftBuff.debuff = false;
            ContentPack.phaseShiftBuff.canStack = false;
            ContentPack.phaseShiftBuff.displayPercentage = false;
            ContentPack.phaseShiftBuff.hidden = false;

            SurvivorActions.onPlayerStartAction += OnLocalRolespawn;
        }

        /// <summary>
        /// При респауне локального игрока заново навешиваем persistent-эффекты
        /// (например, модификатор урона Phase Mark через DamageDealtMessage).
        /// </summary>
        private static void OnLocalRolespawn()
        {
            PhaseDamageHook.EnsureInstalled();
        }
    }

    /// <summary>
    /// Модификатор входящего урона для врагов с Phase Mark (+15%, спецификация 5.1).
    /// Реализован через DamageDealtMessage (R2API/EntityStates pattern) — устанавливается
    /// один раз при первом респауне локального игрока, идемпотентно.
    /// </summary>
    internal static class PhaseDamageHook
    {
        private static bool _installed;

        public static void EnsureInstalled()
        {
            if (_installed) return;
            _installed = true;

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                var phaseMark = ContentPack.phaseMarkBuff;
                if (phaseMark != null && self && !self.isNull && damageInfo != null &&
                    !damageInfo.rejectionReason)
                {
                    var victimBody = self.GetComponent<CharacterBody>();
                    if (victimBody && victimBody.HasBuff(phaseMark))
                    {
                        damageInfo.damage *= 1.15f;
                    }
                }
                orig(self, damageInfo);
            };
        }
    }
}
