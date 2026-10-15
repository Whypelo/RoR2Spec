using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Survivors
{
    /// <summary>
    /// Компонент CharacterMaster Specter. Наследует стандартное поведение мастера;
    /// здесь задаётся стартовый набор (пока пустой — этап 5) и уровень сложности Medium.
    /// </summary>
    public class SpecterMaster : CharacterMaster
    {
        public override void Awake()
        {
            base.Awake();
            name = "SpecterMaster";
        }

        /// <summary>Хук при онлайновом старте мастера.</summary>
        public static void InitializeMasterComponents(CharacterMaster master)
        {
            if (!master) return;

            var inventory = master.GetInventory();
            if (inventory)
            {
                // Стартовых предметов нет: баланс строится на механике фаз.
            }
        }
    }
}
