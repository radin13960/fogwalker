using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Save;
using FogWalker.Settings;
using UnityEngine;

namespace FogWalker.Gameplay.Player
{
    /// <summary>
    /// دوربین سوم‌شخص روی‌شانه: حساسیت/معکوس از تنظیمات، شانه چپ/راست،
    /// Camera Collision با SphereCast (عدم عبور از دیوار)، زوم Aim بر اساس سلاح، لگد و لرزش.
    /// </summary>
    public sealed class PlayerCameraController : MonoBehaviour
    {
        [Header("سیم‌کشی")]
        [SerializeField] private Camera targetCamera;
        [SerializeField, Tooltip("نقطه تکیه بالای شانه (بچه سینه)")]
        private Transform pivot;

        [Header("هندسه")]
        [SerializeField] private Vector3 shoulderOffsetHip = new Vector3(0.55f, 1.55f, -3.1f);
        [SerializeField] private Vector3 shoulderOffsetAim = new Vector3(0.5f, 1.5f, -1.7f);
        [SerializeField] private float cameraCollisionRadius = 0.25f;
        [SerializeField] private float minDistance = 0.3f;

        [Header("کرنای زاویه")]
        [SerializeField] private float pitchMin = -55f;
        [SerializeField] private float pitchMax = 70f;

        [Header("FOV")]
        [SerializeField] private float baseFov = 65f;
        [SerializeField] private float fovLerpSpeed = 10f;
        [SerializeField] private float positionLerpSpeed = 14f;

        /// <summary>چرخش افقی فعلی (Yaw).</summary>
        public float Yaw { get; private set; }
        /// <summary>چرخش عمودی فعلی (Pitch).</summary>
        public float Pitch { get; private set; }
        /// <summary>دوربین اصلی بازی.</summary>
        public Camera MainCamera => targetCamera;
        /// <summary>آیا شانه راست فعال است؟</summary>
        public bool RightShoulder { get; private set; } = true;

        private float _aimBlend; // 0=هیپ 1=هدف
        private bool _aiming;
        private float _aimFovMultiplier = 1f;
        private SettingsManager _settings;
        private float _recoilPitch;
        private float _recoilYaw;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponentInChildren<Camera>(true);
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _settings);
            Yaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            if (GameplayInputSource.Current == null) return;

            var data = _settings != null ? _settings.Data : null;
            float sensitivity = data != null ? data.cameraSensitivity : 1f;
            bool invertY = data != null && data.invertY;
            bool shakeEnabled = data == null || data.cameraShake;

            Vector2 look = GameplayInputSource.Current.LookDelta * 0.12f * sensitivity;
            Yaw += look.x;
            Pitch += (invertY ? look.y : -look.y);

            // اعمال لگد بازگشتی (از شلیک‌ها)
            Yaw += _recoilYaw;
            Pitch += _recoilPitch;
            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, Time.deltaTime * 8f);
            _recoilYaw = Mathf.Lerp(_recoilYaw, 0f, Time.deltaTime * 8f);

            Pitch = Mathf.Clamp(Pitch, pitchMin, pitchMax);

            // مبنای براکت دوربین: روت خود بازیکن
            Vector3 pivotPos = pivot != null ? pivot.position : transform.position + Vector3.up * 1.5f;
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);

            // آفست شانه (Aim نزدیک‌تر)
            _aimBlend = Mathf.MoveTowards(_aimBlend, _aiming ? 1f : 0f, Time.deltaTime * 8f);
            Vector3 offset = Vector3.Lerp(
                ShoulderSign(shoulderOffsetHip), ShoulderSign(shoulderOffsetAim), _aimBlend);
            Vector3 desired = pivotPos + rot * offset;

            // برخورد دوربین با محیط
            Vector3 dir = desired - pivotPos;
            float dist = dir.magnitude;
            Vector3 finalPos = desired;
            if (dist > 0.0001f &&
                Physics.SphereCast(pivotPos, cameraCollisionRadius, dir.normalized, out RaycastHit hit, dist, GameplayLayers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                finalPos = pivotPos + dir.normalized * Mathf.Max(hit.distance - 0.05f, minDistance);
            }

            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, finalPos, Time.deltaTime * positionLerpSpeed);
            targetCamera.transform.rotation = rot;

            // FOV زوم Aim
            float targetFov = baseFov * Mathf.Lerp(1f, _aimFovMultiplier, _aimBlend);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, Time.deltaTime * fovLerpSpeed);

            // لرزش دوربین (Shake) — جمع‌بری در ShakeOffset
            if (shakeEnabled)
                CameraShaker.ApplyTick(targetCamera.transform, rot);
            else
                CameraShaker.ResetTick(targetCamera.transform, rot);
        }

        private Vector3 ShoulderSign(Vector3 v) => new Vector3(RightShoulder ? v.x : -v.x, v.y, v.z);

        /// <summary>تنظیم وضعیت Aim و زوم سلاح فعلی.</summary>
        public void SetAiming(bool aiming, float aimFovMultiplier)
        {
            _aiming = aiming;
            _aimFovMultiplier = aimFovMultiplier;
        }

        /// <summary>اعمال لگد یک شلیک (پیکسل به درجه).</summary>
        public void AddRecoil(float pitchDegrees, float yawDegrees)
        {
            _recoilPitch -= pitchDegrees;
            _recoilYaw += yawDegrees;
        }

        /// <summary>تعویض شانه چپ/راست.</summary>
        public void SetShoulder(bool right) => RightShoulder = right;
    }
}
