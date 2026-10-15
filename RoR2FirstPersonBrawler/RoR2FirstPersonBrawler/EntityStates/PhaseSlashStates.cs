using RoR2;
using RoR2FirstPersonBrawler.Skills.Primary;
using UnityEngine;

namespace RoR2FirstPersonBrawler.EntityStates
{
    /// <summary>
    /// Стадии комбо Phase Slash. Каждое нажатие Primary входит в состояние своей стадии;
    /// урон наносится на фиксированном тайминге (hitTime), комбо сбрасывается по таймауту 1.2 сек.
    /// Анимации подключаются на этапе 3 — сейчас используются стандартные слоты Mercenary-like.
    /// </summary>
    public class PhaseSlashStage1 : EntityState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            _stage = PhaseSlashCombo.AdvanceCombo(gameObject);
            _timer = 0f;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _timer += fixedDeltaTime;
            if (!_hit && _timer >= HitTime)
            {
                _hit = true;
                float dmg; float radius; bool aoe;
                PhaseSlashCombo.GetStageParams(_stage, out dmg, out radius, out aoe);
                bool empowered = PhaseSlashCombo.IsEmpoweredNext(gameObject);
                PhaseSlash.DoSlash(gameObject, dmg, radius, aoe, empowered);
                if (empowered) PhaseSlashCombo.ConsumeEmpowerment(gameObject);
            }
        }

        public override void OnExit(EXIT_STATE_REASON reason)
        {
            base.OnExit(reason);
            // Переход на следующую стадию комбо при повторном нажатии обрабатывает SkillFamily.
        }

        private const float HitTime = 0.12f;
        private int _stage;
        private float _timer;
        private bool _hit;
    }

    public class PhaseSlashStage2 : PhaseSlashStage1 { }

    public class PhaseSlashStage3 : PhaseSlashStage1
    {
        public override void OnEnter()
        {
            // Третья стадия всегда AoE + Phase Mark (спецификация 5.1).
            base.OnEnter();
        }
    }
}
