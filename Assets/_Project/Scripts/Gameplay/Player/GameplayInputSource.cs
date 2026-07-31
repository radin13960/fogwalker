using FogWalker.Controls;
using FogWalker.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FogWalker.Gameplay.Player
{
    /// <summary>
    /// منبع ورودی گیم‌پلی یکپارچه: ادغام Input System (کیبورد/گیم‌پد برای تست) و کنترل‌های لمسی UI.
    /// نمونه واحد زمان‌اجرا از طریق <see cref="Current"/> در دسترس است (استثنای مستند برای مسیر داغ ورودی).
    /// مصرف‌کننده‌ها در Update می‌خوانند؛ لبه‌ها و دلتای Look در LateUpdate پاک می‌شوند.
    /// </summary>
    public sealed class GameplayInputSource : MonoBehaviour
    {
        // ضرایب تبدیل دلتای ورودی به «پیکسل مجازی» تا هر سه منبع حس یکسانی بدهند
        private const float MouseToPixel = 1.6f;
        private const float GamepadLookSpeed = 220f;

        /// <summary>نمونه فعال فعلی (هر مرحله یکی).</summary>
        public static GameplayInputSource Current { get; private set; }

        /// <summary>بردار حرکت نرمال شده.</summary>
        public Vector2 Move { get; private set; }
        /// <summary>دکلای لook این فریم (پیکسل مجازی؛ در دوربین به حساسیت تبدیل می‌شود).</summary>
        public Vector2 LookDelta { get; private set; }
        /// <summary>شلیک نگه داشته شده؟</summary>
        public bool FireHeld { get; private set; }
        /// <summary>لبه فشردن شلیک.</summary>
        public bool FirePressed { get; private set; }
        /// <summary>Aim نگه داشته شده؟</summary>
        public bool AimHeld { get; private set; }
        /// <summary>Sprint نگه داشته شده؟</summary>
        public bool SprintHeld { get; private set; }

        // لبه‌های دکمه
        /// <summary>لبه Reload.</summary>
        public bool ReloadPressed { get; private set; }
        /// <summary>لبه پرش.</summary>
        public bool JumpPressed { get; private set; }
        /// <summary>لبه Crouch.</summary>
        public bool CrouchPressed { get; private set; }
        /// <summary>لبه کاور.</summary>
        public bool CoverPressed { get; private set; }
        /// <summary>لبه تعامل.</summary>
        public bool InteractPressed { get; private set; }
        /// <summary>لبه تعویض سلاح بعدی/قبلی.</summary>
        public int WeaponCycleDelta { get; private set; }
        /// <summary>لبه نارنجک.</summary>
        public bool GrenadePressed { get; private set; }

        private readonly InputAction[] _actionRefs = new InputAction[14];
        private enum A { Move, Look, Fire, Aim, Reload, Jump, Crouch, Cover, Interact, WNext, WPrev, Grenade, Sprint, Pause }
        private bool _actionsReady;

        private void Awake()
        {
            Current = this;
        }

        private void Start()
        {
            CacheActions();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void CacheActions()
        {
            _actionsReady = false;
            if (!ServiceLocator.TryGet(out InputManager input)) return;
            string[] names = { "Move", "Look", "Fire", "Aim", "Reload", "Jump", "Crouch", "Cover", "Interact", "WeaponNext", "WeaponPrev", "Grenade", "Sprint", "Pause" };
            for (int i = 0; i < names.Length; i++)
                _actionRefs[i] = input.GetAction(names[i], onlyGameplayMap: true);
            _actionsReady = true;
        }

        private void Update()
        {
            if (!_actionsReady || !inputGateOpen()) return;
            if (GameplayInputContext.TouchActive) return; // وقتی لمسی فعال است، نوشتن از UI اولویت دارد

            Vector2 move = Read((int)A.Move);
            if (move.sqrMagnitude > 0f) Move = Vector2.ClampMagnitude(move, 1f);

            Vector2 look = Read((int)A.Look);
            if (look.sqrMagnitude > 0f)
            {
                // ماوس دلتاست؛ گیم‌پد سرعت است
                if (look.magnitude < 8f) LookDelta += look * GamepadLookSpeed * Time.deltaTime;
                else LookDelta += look * MouseToPixel;
            }

            if (Held((int)A.Fire)) FireHeld = true;
            if (Pressed((int)A.Fire)) { FirePressed = true; FireHeld = true; }
            if (Held((int)A.Aim)) AimHeld = true;
            if (Held((int)A.Sprint)) SprintHeld = true;
            if (Pressed((int)A.Reload)) ReloadPressed = true;
            if (Pressed((int)A.Jump)) JumpPressed = true;
            if (Pressed((int)A.Crouch)) CrouchPressed = true;
            if (Pressed((int)A.Cover)) CoverPressed = true;
            if (Pressed((int)A.Interact)) InteractPressed = true;
            if (Pressed((int)A.Grenade)) GrenadePressed = true;
            if (Pressed((int)A.WNext)) WeaponCycleDelta = 1;
            if (Pressed((int)A.WPrev)) WeaponCycleDelta = -1;
        }

        private void LateUpdate()
        {
            // پاک‌سازی لبه‌ها و دلتا برای فریم بعد (پس از مصرف توسط همه سیستم‌ها)
            LookDelta = Vector2.zero;
            FireHeld = false;
            FirePressed = false;
            AimHeld = false;
            SprintHeld = false;
            ReloadPressed = false;
            JumpPressed = false;
            CrouchPressed = false;
            CoverPressed = false;
            InteractPressed = false;
            WeaponCycleDelta = 0;
            GrenadePressed = false;
            Move = Vector2.zero;
        }

        private bool inputGateOpen()
        {
            // نقشه خاموش یعنی گیم‌پلی فعال نیست
            return !ServiceLocator.TryGet(out InputManager input) || input.GameplayInputEnabled;
        }

        private Vector2 Read(int i) => _actionRefs[i] != null ? _actionRefs[i].ReadValue<Vector2>() : Vector2.zero;
        private bool Held(int i) => _actionRefs[i] != null && _actionRefs[i].IsPressed();
        private bool Pressed(int i) => _actionRefs[i] != null && _actionRefs[i].WasPressedThisFrame();

        // ---------- API نوشتن از UI لمسی ----------

        /// <summary>جوی‌استیک حرکت (هر فریم هنگام لمس).</summary>
        public void TouchMove(Vector2 value) { Move = Vector2.ClampMagnitude(value, 1f); }
        /// <summary>افزودن دلتای نگاه از ناحیه لمس.</summary>
        public void TouchLook(Vector2 deltaPixels) { LookDelta += deltaPixels; }
        /// <summary>دکمه شلیک (وضعیت).</summary>
        public void TouchFire(bool held) { if (held && !FireHeld) FirePressed = true; FireHeld = held; }
        /// <summary>هدف‌گیری (وضعیت).</summary>
        public void TouchAim(bool held) { AimHeld = held; }
        /// <summary>دویدن (وضعیت).</summary>
        public void TouchSprint(bool held) { SprintHeld = held; }
        /// <summary>لبه‌ها.</summary>
        public void TouchPressReload() { ReloadPressed = true; }
        public void TouchPressJump() { JumpPressed = true; }
        public void TouchPressCrouch() { CrouchPressed = true; }
        public void TouchPressCover() { CoverPressed = true; }
        public void TouchPressInteract() { InteractPressed = true; }
        public void TouchCycleWeapon(int dir) { WeaponCycleDelta = dir; }
        public void TouchPressGrenade() { GrenadePressed = true; }
    }

    /// <summary>فلگ «ورودی لمسی فعال است» — با اولین لمس روشن می‌شود؛ پیش‌فرض خاموش تا تست صفحه‌کلید/موس در Editor کار کند.</summary>
    public static class GameplayInputContext
    {
        public static bool TouchActive { get; set; } = false;
    }
}
