using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// Хранилище иконок способностей. Этап 1-2: процедурные заглушки.
    /// Этап 5: заполняется спрайтами из AssetBundle (Assets/skill_icons).
    /// </summary>
    public static class Icons
    {
        private static Texture2D _phaseSlashIcon;
        private static Texture2D _riftStrikeIcon;
        private static Texture2D _phaseShiftIcon;
        private static Texture2D _voidEruptionIcon;

        public static Texture2D PhaseSlash
        {
            get
            {
                if (!_phaseSlashIcon) _phaseSlashIcon = SpecterSurvivor.GeneratePlaceholderIcon(new Color(0.9f, 0.4f, 0.9f));
                return _phaseSlashIcon;
            }
        }
        public static Texture2D RiftStrike { get { if (!_riftStrikeIcon) _riftStrikeIcon = SpecterSurvivor.GeneratePlaceholderIcon(new Color(0.4f, 0.8f, 0.95f)); return _riftStrikeIcon; } }
        public static Texture2D PhaseShift { get { if (!_phaseShiftIcon) _phaseShiftIcon = SpecterSurvivor.GeneratePlaceholderIcon(new Color(0.6f, 0.6f, 0.9f)); return _phaseShiftIcon; } }
        public static Texture2D VoidEruption { get { if (!_voidEruptionIcon) _voidEruptionIcon = SpecterSurvivor.GeneratePlaceholderIcon(new Color(0.3f, 0.1f, 0.4f)); return _voidEruptionIcon; } }

        /// <summary>Подменяет заглушки на настоящие спрайты (этап 5).</summary>
        public static void ReplaceWithBundleIcons(AssetBundle bundle)
        {
            if (!bundle) return;
            _phaseSlashIcon = bundle.LoadAsset<Texture2D>("specter_icon_phaseSlash");
            _riftStrikeIcon = bundle.LoadAsset<Texture2D>("specter_icon_riftStrike");
            _phaseShiftIcon = bundle.LoadAsset<Texture2D>("specter_icon_phaseShift");
            _voidEruptionIcon = bundle.LoadAsset<Texture2D>("specter_icon_voidEruption");
        }
    }
}
