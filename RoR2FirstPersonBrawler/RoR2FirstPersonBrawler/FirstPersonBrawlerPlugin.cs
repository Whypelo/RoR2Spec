using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using R2API;
using R2API.Utils;
using UnityEngine;

namespace RoR2FirstPersonBrawler
{
    /// <summary>
    /// Точка входа мода "Specter" — сурвайвор ближнего боя с видом от первого лица.
    /// Инициализация разделена на две фазы (Load / Start) согласно рекомендациям R2API:
    /// регистрация контента выполняется в Awake, а добавление в игровые таблицы — в Start.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [R2APISubmoduleDependency(nameof(SurvivorAPI), nameof(SkillAPI),
        nameof(LanguageAPI), nameof(PrefabAPI))]
    public class FirstPersonBrawlerPlugin : BaseUnityPlugin
    {
        // ---- Идентификаторы мода -----------------------------------------------------
        public const string PluginGUID = "com.author.ror2firstpersonbrawler";
        public const string PluginName = "FirstPersonBrawler";
        public const string PluginVersion = "1.0.0";

        /// <summary>Префикс всех AssetBundle-имён и токенов мода.</summary>
        public const string ModelPackPrefix = "specter";

        internal static ManualLogSource Logger;
        internal static FirstPersonBrawlerPlugin Instance;

        // ---- Конфиг: FirstPerson ------------------------------------------------------
        public static ConfigEntry<bool> FP_Enabled;
        public static ConfigEntry<KeyboardShortcut> FP_ToggleKey;
        public static ConfigEntry<float> FP_FOV;
        public static ConfigEntry<float> FP_ClipValue;
        public static ConfigEntry<bool> FP_HideBody;
        public static ConfigEntry<bool> FP_HideHead;

        // ---- Конфиг: Camera -----------------------------------------------------------
        public static ConfigEntry<float> Cam_HeadBobIntensity;
        public static ConfigEntry<bool> Cam_AttackShake;
        public static ConfigEntry<bool> Cam_PhaseShiftEffects;

        // ---- Конфиг: Gameplay (баланс Specter, см. таблицу 4.1 спецификации) ----------
        public static ConfigEntry<float> SpecterBaseHealth;
        public static ConfigEntry<float> SpecterHealthPerLevel;
        public static ConfigEntry<float> SpecterBaseDamage;
        public static ConfigEntry<float> SpecterDamagePerLevel;
        public static ConfigEntry<float> SpecterArmor;
        public static ConfigEntry<float> SpecterMoveSpeed;

        private void Awake()
        {
            Logger = Log;
            Instance = this;

            LoadConfig();

            try
            {
                // Порядок важен: сначала способности (скилл-иконки нужны телу),
                // затем тело/мастер/дисплей, затем регистрация сурвайвора.
                Survivors.SpecterSkills.RegisterSkills();
                Survivors.SpecterSurvivor.RegisterContent();

                LanguageLoader.RegisterLocalizations();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to initialize Specter content: {e}");
                return;
            }

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded. Specter is ready for deployment.");
        }

        private void Start()
        {
            try
            {
                // Добавляем сурвайвора в ротацию только когда игра готова принять контент.
                Survivors.SpecterSurvivor.AddToGame();

                // Harmony-патчи камеры от первого лица (этап 4).
                if (FP_Enabled.Value)
                {
                    Patches.CameraPatches.Apply();
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed during Start(): {e}");
            }
        }

        private void LoadConfig()
        {
            var catFP = "FirstPerson";
            FP_Enabled = Config.Bind(catFP, "Enabled", true,
                "Включает вид от первого лица для Specter при спавне.");
            FP_ToggleKey = Config.Bind(catFP, "ToggleKey", KeyboardShortcut.New(new[] { KeyCode.O }),
                "Клавиша переключения вида от первого лица.");
            FP_FOV = Config.Bind(catFP, "FOV", 90f, new ConfigDescription(
                "Угол обзора камеры.", new AcceptableValueRange<float>(45f, 140f)));
            FP_ClipValue = Config.Bind(catFP, "ClipValue", 0.15f, new ConfigDescription(
                "Near clip plane камеры. Меньше — ближе к стенам, но возможны артефакты.",
                new AcceptableValueRange<float>(0.01f, 1f)));
            FP_HideBody = Config.Bind(catFP, "HideBody", false,
                "Полностью скрывать тело персонажа в FP-режиме.");
            FP_HideHead = Config.Bind(catFP, "HideHead", true,
                "Скрывать модель головы в FP-режиме (рекомендуется).");

            var catCam = "Camera";
            Cam_HeadBobIntensity = Config.Bind(catCam, "HeadBobIntensity", 0.02f,
                "Интенсивность покачивания головы при ходьбе.");
            Cam_AttackShake = Config.Bind(catCam, "AttackShake", true,
                "Лёгкое смещение камеры при ударах комбо.");
            Cam_PhaseShiftEffects = Config.Bind(catCam, "PhaseShiftEffects", true,
                "Эффекты размытия/FOV-буста во время Phase Shift.");

            var catStats = "SpecterStats";
            SpecterBaseHealth = Config.Bind(catStats, "BaseHealth", 130f, "Базовое здоровье.");
            SpecterHealthPerLevel = Config.Bind(catStats, "HealthPerLevel", 39f, "Здоровье за уровень.");
            SpecterBaseDamage = Config.Bind(catStats, "BaseDamage", 14f, "Базовый урон.");
            SpecterDamagePerLevel = Config.Bind(catStats, "DamagePerLevel", 2.8f, "Урон за уровень.");
            SpecterArmor = Config.Bind(catStats, "Armor", 20f, "Броня.");
            SpecterMoveSpeed = Config.Bind(catStats, "MoveSpeed", 7f, "Базовая скорость передвижения.");
        }

        /// <summary>
        /// Загружает ассеты из встроенного AssetBundle (модель/иконки/звуки).
        /// Возвращает null, если бандл ещё не собран — код должен корректно
        /// работать с заглушками на этапах 1-2 разработки.
        /// </summary>
        public static AssetBundle LoadAssetBundle()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "specter_assets");
            if (File.Exists(path))
            {
                return AssetBundle.LoadFromFile(path);
            }
            foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (b && b.name.Contains(ModelPackPrefix)) return b;
            }
            return null;
        }
    }
}
