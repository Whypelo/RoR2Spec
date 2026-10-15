using RoR2;
using RoR2FirstPersonBrawler.Skills.Special;
using UnityEngine;

namespace RoR2FirstPersonBrawler.EntityStates
{
    /// <summary>
    /// Состояние Void Eruption (спецификация 5.4): зарядка ChargeTime секунд,
    /// затем высвобождение BlastAttack радиусом 10 м. Прерывание отменяет волну.
    /// </summary>
    public class VoidEruptionState : EntityState
    {
        private float _chargeTimer;
        private bool _fired;

        public override void OnEnter()
        {
            base.OnEnter();
            _chargeTimer = 0f;
            _fired = false;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _chargeTimer += fixedDeltaTime;

            if (!_fired && isAuthority && _chargeTimer >= VoidEruption.ChargeTime)
            {
                _fired = true;
                VoidEruption.Fire(gameObject);
            }
        }
    }
}
