using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.CharacterAI;
using R2API;
using UnityEngine;
// Примечание: `using UnityEngine.Networking;` не требуется — в актуальных билдах
// RoR2 (Unity 2019.4+) типы NetworkIdentity/NetworkServer находятся в
// Assembly-CSharp.dll, отдельной сборки UnityEngine.Networking в Managed\ нет.

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// Регистрация Specter через SurvivorAPI по образцу HenryTutorial:
    /// master -> body -> display, статы из конфига, AI-слои и ростер.
    /// </summary>
    public static class SpecterSurvivor
    {
        // Имена должны совпадать с токенами LanguageAPI (см. LanguageLoader).
        public const string LanguageTokenName = "HUD_SPECTER_NAME";
        public const string LanguageTokenDesc = "HUD_SPECTER_DESC";

        private static BodyAsset _bodyAsset;
        private static bool _registered;

        /// <summary>Фаза Awake(): создаём префабы и регистрируем контент.</summary>
        public static void RegisterContent()
        {
            if (_registered) return;
            _registered = true;

            var masterGo = new GameObject("SpecterMaster");
            var bodyGo = new GameObject("SpecterBody");
            var displayGo = SpecterDisplay.CreatePlaceholderDisplay();

            var master = CreateMaster(masterGo);
            ContentPack.masterPrefab = master;
            ContentPack.bodyGameObject = bodyGo;
            ContentPack.displayGameObject = displayGo;
            ContentPack.BodyTemplate = CreateBody(bodyGo, displayGo);

            // BodyAsset требует готовые префабы тела/дисплея — добавляем после CreateBody.
            var legacyMasterEditor = masterGo.GetComponent<LegacyMasterEditor>();
            legacyMasterEditor.bodyAssets = new List<BodyAsset> { CreateBodyAsset() };

            SurvivorAPI.Add(ContentPack.masterPrefab);

            FirstPersonBrawlerPlugin.Logger?.LogInfo("Specter content registered (SurvivorAPI).");
        }

        /// <summary>Фаза Start(): добавляем сурвайвора в игровые таблицы.</summary>
        public static void AddToGame()
        {
            if (!ContentPack.masterPrefab) return;

            // Хук инициализации тела — вызывается для каждого спавна Specter.
            On.RoR2.CharacterBody.Start += CharacterBody_Start;
            On.RoR2.CharacterMaster.OnNetworkStart += CharacterMaster_OnNetworkStart;
        }

        // ---------------------------------------------------------------------------
        // Master
        // ---------------------------------------------------------------------------
        private static CharacterMaster CreateMaster(GameObject go)
        {
            var master = go.AddComponent<CharacterMaster>();
            go.AddComponent<NetworkIdentity>();
            go.AddComponent<RoR2.SendRPCBehaviours.SendRpcClientToServer>();
            go.AddComponent<RoR2.SendRPCBehaviours.SendRpcServerToClient>();

            var stateMachine = new EntityStateMachine();
            var aiComponent = go.AddComponent<GenericAI>();
            aiComponent.entityStateRoleFSM = stateMachine;
            aiComponent.entityStateDeploymentFSM = new EntityStateMachine();
            aiComponent.entityStateSwarmFSM = new EntityStateMachine();

            var inventory = go.AddComponent<Inventory>();
            var bodyLocator = go.AddComponent<BodyLocator>();
            var appearance = go.AddComponent<MasterAppearanceProvider>();

            var editor = go.AddComponent<LegacyMasterEditor>();
            editor.bodyAssets = new List<BodyAsset>(); // заполняется после создания тела
            editor.baseMoveSpeed = FirstPersonBrawlerPlugin.SpecterMoveSpeed.Value;
            editor.cameraOffset = new Vector3(0f, 1.5f, 0f);
            editor.hasNoAvailableBodiesOverride = false;
            editor.unlocksDefaultBodies = false;
            editor.autoAssign = true;
            editor.defaultOnStartEntityStates = Array.Empty<Type>();
            editor.defaultReviveOnStartedStates = Array.Empty<Type>();

            return master;
        }

        private static BodyAsset CreateBodyAsset()
        {
            return new BodyAsset
            {
                bodyNameToken = LanguageTokenName,
                bodyPrefab = ContentPack.bodyGameObject,
                displayPrefab = ContentPack.displayGameObject,
                descriptionToken = LanguageTokenDesc,
                icon = GeneratePlaceholderIcon(new Color(0.25f, 0.7f, 0.9f)),
                associatedLockable = null
            };
        }

        // ---------------------------------------------------------------------------
        // Body
        // ---------------------------------------------------------------------------
        private static SpecterBody CreateBody(GameObject bodyGo, GameObject displayGo)
        {
            var body = bodyGo.AddComponent<SpecterBody>();
            bodyGo.AddComponent<NetworkIdentity>();
            bodyGo.AddComponent<RoR2.SendRPCBehaviours.SendRpcServerToClient>();
            bodyGo.AddComponent<RoR2.SendRPCBehaviours.SendRpcClientToServer>();

            var editor = bodyGo.AddComponent<LegacyBodyEditor>();
            var modelLocator = bodyGo.AddComponent<ModelLocator>();
            var childLocator = bodyGo.AddComponent<ChildLocator>();
            var networkCharacterMotor = bodyGo.AddComponent<NetworkUserCharacterMotor>();
            var moveController = bodyGo.AddComponent<RoR2.Move.CharacterMotorMovementController>();
            var aimLogger = bodyGo.AddComponent<AimLogger>();
            var inputBanker = bodyGo.AddComponent<InputBanker>();
            var hitScan = bodyGo.AddComponent<HitScanAttackTracker>();
            var rooster = bodyGo.AddComponent<RoR2.EntityProviders.CharacterRosterElement>();

            var ai = bodyGo.AddComponent<GenericAI>();
            var stateMachine = new EntityStateMachine();
            ai.entityStateRoleFSM = stateMachine;
            ai.findTargetableMonstersToggleButton =
                GenericAI.FindTargetableMonstersToggleButton.Easy;
            ai.assignTeam = TeamMask.teamNone;

            // Слоты способностей
            var slotsGO = new GameObject("SkillSlots");
            slotsGO.transform.SetParent(bodyGo.transform, false);
            var slots = slotsGO.AddComponent<CharacterSkillSlots>();
            slots.hideOnDeath = true;
            SkillAnimatorParameters skillAnimatorParameters;
            SkillLocator.SkillSlotIdentifiers skillSlotIdentifiers;
            slots.InitSkills(out skillAnimatorParameters, out skillSlotIdentifiers,
                ContentPack.phaseSlash, ContentPack.riftStrike,
                ContentPack.phaseShift, ContentPack.voidEruption);

            var behaviour = bodyGo.AddComponent<SkillLocator.SkillBehaviour>();
            behaviour.skillFamilies = new SkillFamily[]
            {
                slots.primary, slots.secondary, slots.utility, slots.special
            };
            skillAnimatorParameters.PopulateAnimatorParameters();

            var animator = bodyGo.AddComponent<Animator>();
            animator.runtimeAnimatorController = CreatePlaceholderAnimatorController(skillAnimatorParameters);
            modelLocator.modelTransform = displayGo.transform;
            modelLocator.baseAnimationLayers = skillAnimatorParameters.baseAnimationLayers;
            modelLocator.maskedAnimationLayers = skillAnimatorParameters.maskedAnimationLayers;

            // Базовые статы (таблица 4.1 спецификации)
            editor.maxHealth = FirstPersonBrawlerPlugin.SpecterBaseHealth.Value;
            editor.healthPerLevel = FirstPersonBrawlerPlugin.SpecterHealthPerLevel.Value;
            editor.baseDamage = FirstPersonBrawlerPlugin.SpecterBaseDamage.Value;
            editor.damagePerLevel = FirstPersonBrawlerPlugin.SpecterDamagePerLevel.Value;
            editor.armor = FirstPersonBrawlerPlugin.SpecterArmor.Value;
            editor.moveSpeed = FirstPersonBrawlerPlugin.SpecterMoveSpeed.Value;
            editor.baseAttackCooldown = 0.5f;
            editor.cooldownPerLevel = 0.9f;
            editor.cooldownScale = 0f;
            editor.isChampion = false;

            body.ainLayerIndex = LayerIndex.player.value;

            // ChildLocator: точка привязки камеры от первого лица (спецификация 6.1).
            var headEntry = new ChildLocator.NameIdentifierPair
            {
                identifier = Camera.CameraConfig.DefaultHeadBonePath.Split('/').Last(),
                child = displayGo.transform
            };
            childLocator.children = new ChildLocator.NameIdentifierPair[] { headEntry };

            return body;
        }

        private static RuntimeAnimatorController CreatePlaceholderAnimatorController(
            SkillAnimatorParameters parameters)
        {
            // До этапа 3 анимаций нет — пустой контроллер допустим,
            // RoR2 не падает без анимаций melee-скиллов (EntityStates работают сами).
            return null;
        }

        // ---------------------------------------------------------------------------
        // Иконки-заглушки (этап 5 заменит их на спрайты из AssetBundle)
        // ---------------------------------------------------------------------------
        internal static Texture2D GeneratePlaceholderIcon(Color color)
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d < 28 ? color : Color.clear);
            }
            tex.Apply();
            return tex;
        }

        // ---------------------------------------------------------------------------
        // Runtime hooks
        // ---------------------------------------------------------------------------
        private static void CharacterBody_Start(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self is SpecterBody specterBody)
            {
                SpecterBody.OnBodyStartAction(specterBody);
            }
        }

        private static void CharacterMaster_OnNetworkStart(
            On.RoR2.CharacterMaster.orig_OnNetworkStart orig, CharacterMaster self)
        {
            orig(self);
            if (self && self.name == "SpecterMaster")
            {
                SpecterMaster.InitializeMasterComponents(self);
            }
        }
    }
}
