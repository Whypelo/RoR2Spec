using System.Collections.Generic;
using RoR2;
using R2API;
using UnityEngine;

namespace RoR2FirstPersonBrawler
{
    /// <summary>
    /// Регистрация локализации через LanguageAPI (спецификация 7.3):
    /// английский (LanguageTypes.en) как основной и русский (LanguageTypes.ru).
    /// Токены: HUD_SPECTER_*, SKILL_SPECTER_*, BUFF_SPECTER_*.
    /// </summary>
    public static class LanguageLoader
    {
        // Языковые коды RoR2: en — основной, ru — поддерживается игрой.
        private const string LangEn = "en";
        private const string LangRu = "ru";

        private class StringPair
        {
            public readonly string En;
            public readonly string Ru;
            public StringPair(string en, string ru) { En = en; Ru = ru; }
        }

        public static void RegisterLocalizations()
        {
            var entries = BuildEntries();
            foreach (var kvp in entries)
            {
                LanguageAPI.Add(kvp.Key, kvp.Value.En, LangEn);
                LanguageAPI.Add(kvp.Key, kvp.Value.Ru, LangRu);
            }
        }

        private static Dictionary<string, StringPair> BuildEntries()
        {
            return new Dictionary<string, StringPair>
            {
                // --- Сурвайвор -----------------------------------------------------
                {
                    Survivors.SpecterSurvivor.LanguageTokenName,
                    new StringPair("Specter", "Спектр")
                },
                {
                    Survivors.SpecterSurvivor.LanguageTokenDesc,
                    new StringPair("A phase-shifting melee survivor who blinks through danger and punishes marked foes.",
                     "Сурвайвор ближнего боя, скользящий между фазами: неуязвимость, усиленные удары и охота на отмеченных врагов.")
                },

                // --- Primary: Phase Slash ------------------------------------------
                {
                    Survivors.ContentPack.SkillToken("PHASESLASH"),
                    new StringPair("Phase Slash", "Фазовый разрез")
                },
                {
                    Survivors.ContentPack.SkillToken("PHASESLASH_DESCRIPTION"),
                    new StringPair("A three-hit combo with a phase blade. The third hit releases an AoE wave that applies Phase Mark, increasing damage taken by 15% for 3 seconds.",
                     "Комбо из трёх ударов фазовым клинком. Третий удар выпускает волну по площади и накладывает Фазовую метку: +15% получаемого урона на 3 секунды.")
                },

                // --- Secondary: Rift Strike ----------------------------------------
                {
                    Survivors.ContentPack.SkillToken("RIFTSTRIKE"),
                    new StringPair("Rift Strike", "Разрыв")
                },
                {
                    Survivors.ContentPack.SkillToken("RIFTSTRIKE_DESCRIPTION"),
                    new StringPair("Dash 8 meters forward through enemies, dealing 300% damage to each one struck. Hitting at least one enemy reduces the cooldown by 1.5 seconds.",
                     "Рывок на 8 метров сквозь врагов, нанося каждому 300% урона. Попадание хотя бы по одному врагу сокращает перезарядку на 1.5 секунды.")
                },

                // --- Utility: Phase Shift -------------------------------------------
                {
                    Survivors.ContentPack.SkillToken("PHASESHIFT"),
                    new StringPair("Phase Shift", "Фазовое смещение")
                },
                {
                    Survivors.ContentPack.SkillToken("PHASESHIFT_DESCRIPTION"),
                    new StringPair("Become invulnerable and translucent for 2.5 seconds, gaining 30% movement speed. Your next Phase Slash deals double damage and always applies Phase Mark.",
                     "На 2.5 секунды станьте неуязвимым и полупрозрачным, получая +30% к скорости. Следующий Фазовый разрез наносит двойной урон и гарантированно накладывает Фазовую метку.")
                },

                // --- Special: Void Eruption -----------------------------------------
                {
                    Survivors.ContentPack.SkillToken("VOIDERUPTION"),
                    new StringPair("Void Eruption", "Извержение Пустоты")
                },
                {
                    Survivors.ContentPack.SkillToken("VOIDERUPTION_DESCRIPTION"),
                    new StringPair("Charge for 1 second, then erupt void energy in a 10 meter radius for 800% damage. Enemies with Phase Mark take an extra 400% damage and are stunned for 2 seconds.",
                     "Заряжайтесь 1 секунду, затем высвободите энергию пустоты в радиусе 10 метров, нанося 800% урона. Враги с Фазовой меткой получают ещё 400% урона и оглушаются на 2 секунды.")
                },

                // --- Buffs ------------------------------------------------------------
                {
                    "BUFF_SPECTER_PHASEMARK_name",
                    new StringPair("Phase Mark", "Фазовая метка")
                },
                {
                    "BUFF_SPECTER_PHASEMARK_desc",
                    new StringPair("Marked by the phase blade. Takes 15% increased damage from all sources.",
                     "Отмечен фазовым клинком. Получает на 15% больше урона от всех источников.")
                },
                {
                    "BUFF_SPECTER_PHASESHIFT_name",
                    new StringPair("Phase Shifted", "В фазе")
                },
                {
                    "BUFF_SPECTER_PHASESHIFT_desc",
                    new StringPair("Out of phase: invulnerable, faster, and the next attack is empowered.",
                     "Вне фазы: неуязвимость, повышенная скорость, следующая атака усилена.")
                }
            };
        }
    }
}
