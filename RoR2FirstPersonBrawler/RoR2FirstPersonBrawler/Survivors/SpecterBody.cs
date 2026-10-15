using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// CharacterBody Specter: hit-box группы для melee-атак и инициализация
    /// вида от первого лица. Базовые статы задаются LegacyBodyEditor при
    /// построении префаба (см. SpecterSurvivor.CreateBody).
    /// </summary>
    public class SpecterBody : CharacterBody
    {
        [Header("Specter Body")]
        [Tooltip("Ссылка на аниматор модели; заполняется из префаба (этап 3).")]
        public Animator modelAnimator;

        // Флаг усиления следующей Primary-атаки после Phase Shift (спецификация 5.3).
        [HideInInspector] public bool NextAttackEmpowered;

        private Camera.FirstPersonCameraController _fpController;

        public override void Awake()
        {
            base.Awake();

            // Имя тела — ключ в survivorDefaultBoneOverride / survivorClipDefaultValues.
            name = "SpecterBody";
        }

        /// <summary>
        /// Хук On.RoR2.CharacterBody.Start: вызывается для каждого спавна тела.
        /// Для локального игрока за Specter включает камеру от первого лица (спецификация 6.1).
        /// </summary>
        public static void OnBodyStartAction(SpecterBody body)
        {
            if (!body) return;

            EnsureHitBoxGroups(body);

            if (body.isLocalPlayer && FirstPersonBrawlerPlugin.FP_Enabled.Value)
            {
                var cam = UnityEngine.Camera.main;
                if (!cam) return;

                var controller = cam.gameObject.GetComponent<Camera.FirstPersonCameraController>();
                if (!controller)
                {
                    controller = cam.gameObject.AddComponent<Camera.FirstPersonCameraController>();
                }
                body._fpController = controller;
                controller.AttachTo(body);
            }
        }

        private static void EnsureHitBoxGroups(GameObject root)
        {
            // На этапе 1 (болванчик без модели) создаём временные боксы, чтобы
            // OverlapAttack-логика способностей собирала цели по группам "Slash"/"Eruption".
            CreateGroupIfMissing(root, ref ContentPack.slashHitBoxGroup, "Slash",
                new Vector3(0f, 1f, 1.4f), new Vector3(1.2f, 1.4f, 2.2f));

            CreateGroupIfMissing(root, ref ContentPack.eruptionHitBoxGroup, "Eruption",
                Vector3.zero, new Vector3(9f, 3f, 9f));
        }

        private static void CreateGroupIfMissing(GameObject root, ref HitBoxGroup group,
            string groupName, Vector3 center, Vector3 size)
        {
            if (group != null && group.name == groupName && group.hurtBoxGroupLayersMask != 0)
                return;

            var go = new GameObject("HitBoxGroup_" + groupName);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = center;

            var hb = go.AddComponent<BoxCollider>();
            hb.center = Vector3.zero;
            hb.size = size;
            hb.isTrigger = true;
            go.layer = LayerIndex.hitbox.value;

            var hurtGroupGo = new GameObject("HurtBoxGroup_" + groupName);
            hurtGroupGo.transform.SetParent(root.transform, false);

            group = go.AddComponent<HitBoxGroup>();
            group.groupName = groupName;
            group.hurtBoxGroupLayersMask = LayerIndex.HURTBOX_GROUP_MASK;
            group.hurtBoxGroupCollidersMask = 1;
            group.aimVector = Vector3.forward;
        }
    }
}
