using RoR2;
using RoR2FirstPersonBrawler.Skills.Utility;
using UnityEngine;

namespace RoR2FirstPersonBrawler.EntityStates
{
    /// <summary>
    /// Состояние Phase Shift (спецификация 5.3): неуязвимость, +30% скорости,
    /// усиление следующей Primary-атаки. Эффект длится Duration секунд.
    /// </summary>
    public class PhaseShiftState : EntityState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority)
            {
                PhaseShift.Begin(gameObject);
            }
        }

        public override void OnExit(EXIT_STATE_REASON reason)
        {
            base.OnExit(reason);
            if (isAuthority)
            {
                PhaseShift.End(gameObject);
            }
        }
    }
}
