using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// Хранилище игровых токенов и префаб-объектов Specter.
    /// Заполняется во время SpecterSkills.RegisterSkills() / SpecterSurvivor.RegisterContent().
    /// </summary>
    public static class ContentPack
    {
        // ---- Тело / мастер / дисплей --------------------------------------------------
        public static SpecterBody BodyTemplate;
        public static CharacterMaster masterPrefab;
        public static GameObject bodyGameObject;      // CharacterBody prefab (networked)
        public static GameObject displayGameObject;   // Display prefab (visuals only)

        // ---- Способности ---------------------------------------------------------------
        public static SkillDef phaseSlash;    // Primary
        public static SkillDef riftStrike;    // Secondary
        public static SkillDef phaseShift;    // Utility
        public static SkillDef voidEruption;  // Special

        // ---- Баффы ----------------------------------------------------------------------
        public static BuffDef phaseMarkBuff;       // Дебафф уязвимости +15%
        public static BuffDef phaseShiftBuff;      // Активное "фазовое" состояние

        // ---- Hit boxes -------------------------------------------------------------------
        public static HitBoxGroup slashHitBoxGroup;    // "Slash" — комбо и рывок
        public static HitBoxGroup eruptionHitBoxGroup; // "Eruption" — AoE спец-способность

        // ---- Локализация --------------------------------------------------------------
        public const string TokenPrefix = "SPECTER";

        /// <summary>Токен скилла, напр. SKILL_SPECTER_PHASESLASH.</summary>
        public static string SkillToken(string name)
        {
            return string.Format("SKILL_{0}_{1}", TokenPrefix, name.ToUpperInvariant());
        }

        public static string BuffToken(string name)
        {
            return string.Format("BUFF_{0}_{1}", TokenPrefix, name.ToUpperInvariant());
        }

        // Реестр всех LanguageAPI-строк для пакетной регистрации.
        public static readonly Dictionary<string, string> LocalizedStringsEN = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> LocalizedStringsRU = new Dictionary<string, string>();
    }
}
