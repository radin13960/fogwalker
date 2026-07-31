using FogWalker.Gameplay.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FogWalker.UI.HUD
{
    /// <summary>
    /// جوی‌استیک مجازی: خروجی Vector2 تا شعاع تعریف‌شده؛ نوشتن هر فریم در GameplayInputSource.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("سیم‌کشی")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("پیکربندی")]
        [SerializeField, Tooltip("شعاع حرکت دسته (پیکسل UI)")]
        private float radiusPixels = 70f;
        [SerializeField, Tooltip("ناحیه مرد بالایی جوی‌استیک (0..1)")]
        private float deadZone = 0.12f;

        private Vector2 _value;
        private bool _dragging;
        private GameplayInputSource _input;

        private void Awake()
        {
            if (background == null) background = transform as RectTransform;
            _input = FindFirstObjectByType<GameplayInputSource>();
        }

        private void Update()
        {
            if (_input == null) _input = GameplayInputSource.Current;
            if (_dragging && _input != null)
                _input.TouchMove(_value);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GameplayInputContext.TouchActive = true;
            _dragging = true;
            UpdateHandle(eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => UpdateHandle(eventData.position);

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
            _value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        private void UpdateHandle(Vector2 screenPos)
        {
            if (background == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, screenPos, null, out Vector2 local);
            Vector2 clamped = Vector2.ClampMagnitude(local, radiusPixels);
            _value = clamped / radiusPixels;
            if (_value.magnitude < deadZone) { _value = Vector2.zero; clamped = Vector2.zero; }
            if (handle != null) handle.anchoredPosition = clamped;
        }
    }

    /// <summary>
    /// ناحیه آزاد نیمه راست صفحه برای چرخش دوربین؛ دلتای درگ به GameplayInputSource.
    /// </summary>
    public sealed class LookTouchArea : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField, Tooltip("تقسیم دلتای پیکسل به حساسیت دوربین (پیکسل→واحد)")]
        private float pixelScale = 1f;

        private GameplayInputSource _input;

        public void OnPointerDown(PointerEventData eventData)
        {
            GameplayInputContext.TouchActive = true;
            if (_input == null) _input = GameplayInputSource.Current;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_input == null) { _input = GameplayInputSource.Current; if (_input == null) return; }
            _input.TouchLook(eventData.delta * pixelScale);
        }
    }

    /// <summary>
    /// دکمه نگه‌داشتنی (شلیک/Aim/دویدن): رویداد down/up جدا.
    /// </summary>
    public sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum Kind { Fire, Aim, Sprint }

        [SerializeField] private Kind kind = Kind.Fire;
        [SerializeField, Tooltip("بازخورد بصری نگه‌داشتن (تیره شدن)")]
        private Image visual;

        private GameplayInputSource _input;
        private Color _baseColor;

        private void Awake()
        {
            if (visual != null) _baseColor = visual.color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GameplayInputContext.TouchActive = true;
            if (_input == null) _input = GameplayInputSource.Current;
            if (_input == null) return;
            Apply(true);
            SetVisual(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_input == null) return;
            Apply(false);
            SetVisual(false);
        }

        private void Apply(bool held)
        {
            switch (kind)
            {
                case Kind.Fire: _input.TouchFire(held); break;
                case Kind.Aim: _input.TouchAim(held); break;
                case Kind.Sprint: _input.TouchSprint(held); break;
            }
        }

        private void SetVisual(bool held)
        {
            if (visual == null) return;
            Color c = _baseColor;
            c.a = _baseColor.a * (held ? 1.4f : 1f);
            visual.color = c;
        }
    }

    /// <summary>
    /// دکمه لبه‌ای (Reload/تعویض/پرش/کاور/تعامل/نارنجک/Crouch).
    /// </summary>
    public sealed class TouchPressButton : MonoBehaviour, IPointerDownHandler
    {
        public enum Kind { Reload, Jump, Crouch, Cover, Interact, Grenade, WeaponNext, WeaponPrev }

        [SerializeField] private Kind kind = Kind.Reload;

        public void OnPointerDown(PointerEventData eventData)
        {
            GameplayInputContext.TouchActive = true;
            var input = GameplayInputSource.Current;
            if (input == null) return;

            switch (kind)
            {
                case Kind.Reload: input.TouchPressReload(); break;
                case Kind.Jump: input.TouchPressJump(); break;
                case Kind.Crouch: input.TouchPressCrouch(); break;
                case Kind.Cover: input.TouchPressCover(); break;
                case Kind.Interact: input.TouchPressInteract(); break;
                case Kind.Grenade: input.TouchPressGrenade(); break;
                case Kind.WeaponNext: input.TouchCycleWeapon(1); break;
                case Kind.WeaponPrev: input.TouchCycleWeapon(-1); break;
            }
        }
    }
}
