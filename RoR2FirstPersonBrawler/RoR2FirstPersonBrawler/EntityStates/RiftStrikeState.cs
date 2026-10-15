using RoR2;
using RoR2.Motor;
using RoR2FirstPersonBrawler.Skills.Secondary;
using UnityEngine;

namespace RoR2FirstPersonBrawler.EntityStates
{
    /// <summary>
    /// Состояние рывка Rift Strike (спецификация 5.2): движение вперёд на 8 м
    /// через CharacterMotor.Motor с постоянным уроном по врагам вдоль трассы.
    /// Сквозь врагов — коллизии персонажа не блокируют рывок (Motor.MovePosition).
    /// </summary>
    public class RiftStrikeState : EntityState
    {
        private CharacterMotor _motor;
        private Vector3 _target;
        private Vector3 _start;
        private float _elapsed;
        private bool _hitAny;

        public override void OnEnter()
        {
            base.OnEnter();
            var characterMotor = gameObject.GetComponent<RoR2.CharacterMotor>();
            _motor = characterMotor ? characterMotor.Motor.ForwardTranslate : null;
            _start = gameObject.transform.position;
            _target = RiftStrike.ComputeDashTarget(gameObject.transform);
            _elapsed = 0f;
            _hitAny = false;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _elapsed += fixedDeltaTime;
            float t = Mathf.Clamp01(_elapsed / RiftStrike.DashDuration);

            if (_motor)
            {
                // Телепорт по трассе рывка: сквозь врагов и без физ. коллизий персонажа.
                Vector3 desired = Vector3.Lerp(_start, _target, t);
                Vector3 delta = desired - transform.position;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    _motor.MovePosition(delta, false, true);
                }
            }

            // Наносим урон каждые ~0.05 сек рывка (проход сквозь врагов).
            if (isAuthority && _elapsed >= _nextTick)
            {
                _nextTick = _elapsed + 0.05f;
                if (RiftStrike.DashTick(gameObject, gameObject.transform.position))
                {
                    _hitAny = true;
                }
            }

            if (t >= 1f)
            {
                outer.SetState(nextState, "End");
            }
        }

        public override void OnExit(EXIT_STATE_REASON reason)
        {
            base.OnExit(reason);
            if (_hitAny)
            {
                // Попадание хотя бы по одному врагу: -1.5 сек к кулдауну (5.2).
                RiftStrike.ApplyCooldownReduction(gameObject);
            }
        }

        private float _nextTick;
    }
}
