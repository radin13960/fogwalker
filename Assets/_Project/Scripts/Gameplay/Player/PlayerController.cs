using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using UnityEngine;

namespace FogWalker.Gameplay.Player
{
    /// <summary>
    /// حرکت بازیکن با CharacterController (بدون Root Motion برای پایداری برخوردها):
    /// راه‌رفتن/دویدن/خم‌شدن، پرش محدود، گرانش، چرخش به سمت حرکت (یا رو به جلو هنگام Aim)،
    /// اعمال ضریب سرعت Aim، و گزارش صدای قدم برای AI. در حالت کاور، حرکت به CoverController سپرده می‌شود.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("داده")]
        [SerializeField] private PlayerTuningSO tuning;

        [Header("سیم‌کشی")]
        [SerializeField, Tooltip("Animator (اختیاری — در نبودش حذف می‌شود)")]
        private Animator animator;

        private CharacterController _controller;
        private PlayerCameraController _camera;
        private CoverController _cover;
        private HealthComponent _health;

        private Vector3 _velocity;
        private float _turnSmoothVelocity;
        private float _currentSpeed;
        private bool _isCrouching;
        private float _capsuleStandHeight;
        private Vector3 _capsuleStandCenter;
        private float _stepSoundTimer;

        /// <summary>در حال دویدن؟</summary>
        public bool IsSprinting { get; private set; }
        /// <summary>خمیده؟</summary>
        public bool IsCrouching => _isCrouching;
        /// <summary>روی زمین؟</summary>
        public bool IsGrounded { get; private set; }
        /// <summary>سرعت فعلی (برای Blend Tree).</summary>
        public float PlanarSpeed { get; private set; }
        /// <summary>تنظیمات بازیکن.</summary>
        public PlayerTuningSO Tuning => tuning;
        /// <summary>در حال Aim؟ (از کنترلر مبارزه تنظیم می‌شود)</summary>
        public bool IsAiming { get; set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _camera = GetComponent<PlayerCameraController>();
            _cover = GetComponent<CoverController>();
            _health = GetComponent<HealthComponent>();
            _capsuleStandHeight = _controller.height;
            _capsuleStandCenter = _controller.center;
        }

        private void Update()
        {
            if (tuning == null || GameplayInputSource.Current == null) return;
            if (_health != null && !_health.IsAlive) return;
            if (_cover != null && _cover.IsInCover) return; // کاور خودش Move می‌زند

            GameplayInputSource input = GameplayInputSource.Current;

            IsGrounded = _controller.isGrounded;
            if (IsGrounded && _velocity.y < 0f) _velocity.y = -2f;

            // Crouch toggle
            if (input.CrouchPressed)
            {
                _isCrouching = !_isCrouching;
                ApplyCrouchCapsule();
            }

            IsSprinting = input.SprintHeld && !_isCrouching && !IsAiming;

            float targetSpeed = _isCrouching ? tuning.crouchSpeed
                : IsSprinting ? tuning.runSpeed
                : tuning.walkSpeed;
            if (IsAiming) targetSpeed *= tuning.aimSpeedMultiplier;

            Vector2 moveInput = input.Move;
            Vector3 moveDir = CameraRelative(moveInput);
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, moveDir.magnitude * targetSpeed, tuning.acceleration * Time.deltaTime);
            PlanarSpeed = _currentSpeed;

            Vector3 horizontalMove = moveDir.normalized * _currentSpeed;

            // چرخش بدن: هنگام Aim رو به دوربین، وگرنه رو به حرکت
            if (IsAiming && _camera != null)
            {
                float yaw = _camera.Yaw;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            else if (moveDir.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, tuning.turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // پرش محدود
            if (input.JumpPressed && IsGrounded && !_isCrouching && tuning.jumpHeight > 0f)
                _velocity.y = Mathf.Sqrt(tuning.jumpHeight * -2f * tuning.gravity);

            _velocity.y += tuning.gravity * Time.deltaTime;

            _controller.Move((horizontalMove + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);

            // صدای قدم هنگام دویدن (هشدار به AI)
            ReportFootsteps();
            DriveAnimator(moveInput);
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;
            float yaw = _camera != null ? _camera.Yaw : transform.eulerAngles.y;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            return rot * new Vector3(input.x, 0f, input.y);
        }

        private void ApplyCrouchCapsule()
        {
            if (_isCrouching)
            {
                _controller.height = _capsuleStandHeight * 0.55f;
                _controller.center = new Vector3(_capsuleStandCenter.x, _capsuleStandHeight * 0.275f, _capsuleStandCenter.z);
            }
            else
            {
                _controller.height = _capsuleStandHeight;
                _controller.center = _capsuleStandCenter;
            }
        }

        private void ReportFootsteps()
        {
            if (!IsSprinting || !IsGrounded || PlanarSpeed < tuning.walkSpeed) return;
            _stepSoundTimer -= Time.deltaTime;
            if (_stepSoundTimer <= 0f)
            {
                _stepSoundTimer = 0.34f;
                AISoundBus.Report(transform.position, 10f, 0.4f);
            }
        }

        private void DriveAnimator(Vector2 moveInput)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            SafeAnim.SetFloat(animator, "Speed", PlanarSpeed);
            SafeAnim.SetBool(animator, "IsGrounded", IsGrounded);
            SafeAnim.SetBool(animator, "IsAiming", IsAiming);
            SafeAnim.SetFloat(animator, "MoveX", moveInput.x);
            SafeAnim.SetFloat(animator, "MoveY", moveInput.y);
            SafeAnim.SetBool(animator, "IsInCover", _cover != null && _cover.IsInCover);
        }

        /// <summary>اجرای Teleport برای چک‌پوینت/Respawn (CharacterController-safe).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _velocity = Vector3.zero;
            _controller.enabled = true;
        }
    }

    /// <summary>تنظیم امن پارامترهای Animator بدون خطا در نبود پارامتر/کنترلر (Placeholder-friendly).</summary>
    public static class SafeAnim
    {
        public static void SetFloat(Animator a, string p, float v) { if (Has(a, p)) a.SetFloat(p, v); }
        public static void SetBool(Animator a, string p, bool v) { if (Has(a, p)) a.SetBool(p, v); }
        public static void SetTrigger(Animator a, string p) { if (Has(a, p)) a.SetTrigger(p); }

        private static bool Has(Animator a, string param)
        {
            if (a == null || a.runtimeAnimatorController == null) return false;
            var ps = a.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].name == param) return true;
            return false;
        }
    }
}
