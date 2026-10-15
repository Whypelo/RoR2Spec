using System;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Patches
{
    /// <summary>
    /// Harmony-патчи для вида от первого лица (спецификация 6.1):
    /// — CameraRig.OnPreRender: не переписывать позицию камеры, когда FP-контроллер активен;
    /// — MainCameraRenderer: near clip и FOV берутся из CameraConfig/конфига мода для Specter.
    /// Патчи применяются с высоким приоритетом (Priority.High), чтобы корректно
    /// взаимодействовать с другими модами камеры (см. раздел 10 «Риски»).
    /// </summary>
    internal static class CameraPatches
    {
        private static bool _applied;

        public static void Apply()
        {
            if (_applied) return;
            _applied = true;

            try
            {
                var harmony = new Harmony(FirstPersonBrawlerPlugin.PluginGUID + ".camera");
                harmony.PatchAll(typeof(CameraPatches));
                FirstPersonBrawlerPlugin.Logger?.LogInfo("Camera patches applied.");
            }
            catch (Exception e)
            {
                FirstPersonBrawlerPlugin.Logger?.LogError(
                    "Failed to apply camera patches (FP mode limited to runtime controller): " + e);
            }
        }

        // -----------------------------------------------------------------------
        // CameraRig.LateUpdate — оригинал делает плавное следование камеры за игроком.
        // Пока FirstPersonCameraController активен, пропускаем его, т.к. камеру
        // позиционирует контроллер (кость head).
        // -----------------------------------------------------------------------
        [HarmonyPatch(typeof(CameraRig))]
        [HarmonyPatch("LateUpdate")]
        [HarmonyPriority(Priority.High)]
        internal static bool CameraRigOnPreRenderPrefix()
        {
            var cam = UnityEngine.Camera.main;
            if (!cam) return true;
            var controller = cam.GetComponent<Camera.FirstPersonCameraController>();
            return controller == null || !controller.isActiveAndEnabled;
        }

        // -----------------------------------------------------------------------
        // MainCameraRenderer.updateCameraProperties — патчим после оригинала,
        // переписывая nearClipPlane/fieldOfView у реальной камеры в FP-режиме.
        // Так мы не зависим от приватных свойств clipPlane/fieldOfView.
        // -----------------------------------------------------------------------
        [HarmonyPatch(typeof(MainCameraRenderer))]
        [HarmonyPatch("updateCameraProperties")]
        [HarmonyAfter(new[] { "com.brennenmurphy.truefirstperson" })]
        internal static void MainCameraRendererUpdatePostfix()
        {
            var localBody = GetLocalBody();
            if (localBody == null || !(localBody is Survivors.SpecterBody)) return;

            var cam = UnityEngine.Camera.main;
            if (!cam) return;
            var controller = cam.GetComponent<Camera.FirstPersonCameraController>();
            if (!controller || !controller.IsFirstPersonActive) return;

            cam.nearClipPlane = Camera.CameraConfig.GetClipValue(localBody.name);
            cam.fieldOfView = FirstPersonBrawlerPlugin.FP_FOV.Value;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private static CharacterBody GetLocalBody()
        {
            var master = LocalUserLocalMaster.localCharacterMaster;
            return master ? master.GetBody() : null;
        }
    }
}
