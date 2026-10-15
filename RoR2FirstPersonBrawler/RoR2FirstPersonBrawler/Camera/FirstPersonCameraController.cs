using RoR2;
using UnityEngine;

namespace RoR2FirstPersonBrawler.Camera
{
    /// <summary>
    /// Управляет камерой от первого лица для Specter (спецификация 6):
    /// привязка к кости Head, headbob, «выпад» при атаке, FOV-буст рывка,
    /// эффекты Phase Shift, скрытие головы/тела, переключение по клавише.
    /// Монотонный компонент: создаётся один на MainCamera, активен только
    /// когда локальный игрок играет за Specter.
    /// </summary>
    public class FirstPersonCameraController : MonoBehaviour
    {
        private static FirstPersonCameraController _instance;

        private UnityEngine.Camera _unityCamera;
        private Survivors.SpecterBody _attachedBody;
        private Transform _headBone;
        private bool _firstPersonActive;

        /// <summary>Активен ли сейчас режим от первого лица (для Harmony-патчей).</summary>
        public bool IsFirstPersonActive
        {
            get { return _firstPersonActive; }
        }

        // Состояние эффектов
        private float _fovBoost;          // Rift Strike (+10°, затухает)
        private float _lungeOffset;       // Primary "выпад" (затухает)
        private float _phaseShiftBlur;    // 0..1 интенсивность фазовых эффектов
        private float _bobPhase;
        private float _originalFov;
        private float _originalNearClip;

        #region Lifecycle

        public void AttachTo(Survivors.SpecterBody body)
        {
            if (!body) return;
            _attachedBody = body;
            ResolveHeadBone(body);
            SetFirstPerson(FirstPersonBrawlerPlugin.FP_Enabled.Value);
        }

        public void Detach()
        {
            SetFirstPerson(false);
            _attachedBody = null;
            _headBone = null;
        }

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            _unityCamera = GetComponent<UnityEngine.Camera>();
            if (!_unityCamera) _unityCamera = UnityEngine.Camera.main;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            HandleToggleKey();

            if (!_firstPersonActive || !_attachedBody) return;

            // Затухания эффектов
            _fovBoost = Mathf.MoveTowards(_fovBoost, 0f, Time.deltaTime * 40f);
            _lungeOffset = Mathf.MoveTowards(_lungeOffset, 0f, Time.deltaTime * 1.2f);

            float targetBlur = IsInPhaseShift() ? 1f : 0f;
            _phaseShiftBlur = Mathf.MoveTowards(_phaseShiftBlur, targetBlur, Time.deltaTime * 4f);

            UpdateCameraTransform();
            UpdateCameraProjection();
        }

        #endregion

        #region Setup

        private void ResolveHeadBone(Survivors.SpecterBody body)
        {
            _headBone = null;

            // 1. Явный путь из CameraConfig через ChildLocator модели.
            string path = CameraConfig.GetBonePath(body.name);
            var modelRoot = body.modelLocator ? body.modelLocator.modelTransform : null;
            if (modelRoot)
            {
                var found = modelRoot.Find(path);
                if (found) { _headBone = found; }
            }

            // 2. Поиск по приоритету имён: head -> chest (спецификация 6.1).
            if (!_headBone && modelRoot)
            {
                foreach (var boneName in CameraConfig.BoneSearchPriority)
                {
                    _headBone = FindRecursive(modelRoot, boneName);
                    if (_headBone) break;
                }
            }

            // 3. Заглушка этапа 1: над центром капсулы.
            if (!_headBone)
            {
                _headBone = body.transform;
            }
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    return child;
                var result = FindRecursive(child, name);
                if (result) return result;
            }
            return null;
        }

        private void HandleToggleKey()
        {
            var key = FirstPersonBrawlerPlugin.FP_ToggleKey.Value;
            if (!key.IsValid()) return;
            if (!Input.GetKeyDown(key.primary) &&
                (key.modifiers == null || !Input.GetKey(key.modifiers))) return;
            if (!_attachedBody || !_attachedBody.isLocalPlayer) return;

            SetFirstPerson(!_firstPersonActive);
        }

        public void SetFirstPerson(bool active)
        {
            if (_firstPersonActive == active) return;
            _firstPersonActive = active;

            if (!_unityCamera) return;

            if (active)
            {
                _originalFov = _unityCamera.fieldOfView;
                _originalNearClip = _unityCamera.nearClipPlane;
                _unityCamera.nearClipPlane = CameraConfig.GetClipValue(
                    _attachedBody ? _attachedBody.name : null);
                ApplyModelVisibility(true);
            }
            else
            {
                _unityCamera.fieldOfView = _originalFov;
                _unityCamera.nearClipPlane = _originalNearClip;
                ApplyModelVisibility(false);
            }
        }

        /// <summary>Скрытие головы/тела и item displays в FP-режиме (спецификация 6.2).</summary>
        private void ApplyModelVisibility(bool fpMode)
        {
            var body = _attachedBody;
            if (!body || !body.modelLocator || !body.modelLocator.modelTransform) return;

            var display = body.modelLocator.modelTransform.GetComponent<Survivors.SpecterDisplay>();
            if (display)
            {
                if (display.headRenderer && FirstPersonBrawlerPlugin.FP_HideHead.Value)
                    display.headRenderer.enabled = !fpMode;

                if (display.bodyRenderer && FirstPersonBrawlerPlugin.FP_HideBody.Value)
                    display.bodyRenderer.enabled = !fpMode;

                // Клинок остаётся видимым всегда (спецификация 6.2).
                if (display.bladeVFX) display.bladeVFX.SetActive(true);
            }

            // Item displays перекрывают обзор — скрываем их в FP (ограничение 6.4).
            foreach (var rd in body.GetComponentsInChildren<RoR2.ItemDisplay>())
            {
                rd.enabled = !fpMode;
            }
        }

        #endregion

        #region Per-frame updates

        private void UpdateCameraTransform()
        {
            if (!_headBone) return;

            // HeadBob при движении (спецификация 7.2 HeadBobIntensity).
            var motor = _attachedBody ? _attachedBody.GetMovementMotor<CharacterMotor>() : null;
            float speedSqr = motor ? motor.velocity.sqrMagnitude : 0f;
            float bobAmount = FirstPersonBrawlerPlugin.Cam_HeadBobIntensity.Value;
            _bobPhase += Time.deltaTime * Mathf.Sqrt(speedSqr) * 2f;
            float bob = Mathf.Sin(_bobPhase * 6.28318f) * bobAmount *
                        Mathf.Clamp01(speedSqr / 25f);

            Vector3 worldPos = _headBone.position
                + _headBone.up * bob
                + _headBone.forward * _lungeOffset;   // "выпад" при атаке

            transform.position = worldPos;
            // Поворот камеры — за игроком (RoR2 управляет углом через CameraRig);
            // копируем ротацию rig-а вокруг позиции головы.
            var rig = CameraRig.main;
            if (rig)
            {
                transform.rotation = Quaternion.LookRotation(rig.transform.forward, Vector3.up);
            }
        }

        private void UpdateCameraProjection()
        {
            if (!_unityCamera) return;

            float baseFov = FirstPersonBrawlerPlugin.FP_FOV.Value;
            float phaseFov = _phaseShiftBlur > 0f ? -5f * _phaseShiftBlur : 0f;
            _unityCamera.fieldOfView = baseFov + _fovBoost + phaseFov;
        }

        #endregion

        #region External effect hooks (skills -> camera)

        /// <summary>Primary attack lunge (спецификация 6.3).</summary>
        public static void NotifyAttack(GameObject character)
        {
            if (!_instance || !character || _instance._attachedBody != character.GetComponent<Survivors.SpecterBody>())
                return;
            if (!FirstPersonBrawlerPlugin.Cam_AttackShake.Value) return;
            _instance._lungeOffset = CameraConfig.AttackLungeDistance;
        }

        /// <summary>Rift Strike FOV boost (+10°).</summary>
        public static void NotifyDash(GameObject character)
        {
            if (!_instance || !character) return;
            _instance._fovBoost = CameraConfig.DashFovBoost;
        }

        /// <summary>Phase Shift blur/chromatic effects.</summary>
        public static void NotifyPhaseShift(GameObject character, bool active)
        {
            if (!_instance || !character) return;
            if (!FirstPersonBrawlerPlugin.Cam_PhaseShiftEffects.Value) return;
            // Значение плавно доедет до цели в Update(): размытие через post-process
            // подключается на этапе 5; сейчас — лёгкий FOV-дроп как маркер фазы.
            _instance._phaseShiftBlur = active ? 1f : 0f;
        }

        private bool IsInPhaseShift()
        {
            var body = _attachedBody;
            return body && Survivors.ContentPack.phaseShiftBuff != null &&
                   body.HasBuff(Survivors.ContentPack.phaseShiftBuff);
        }

        #endregion
    }
}
