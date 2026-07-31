using UnityEngine;

namespace FogWalker.UI.Common
{
    /// <summary>
    /// تطبیق RectTransform با Safe Area دستگاه (ناچ/پانچ‌هول) — الزامی برای موبایل.
    /// روی یک پنل تمام‌صفحه بگذارید؛ در چرخش Landscape خودکار به‌روز می‌شود.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField, Tooltip("پایین صفحه هم اعمال شود (برای HUD مهم است)")]
        private bool applyBottom = true;

        private RectTransform _rect;
        private Rect _lastSafeArea;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            // فقط وقتی چیزی عوض شد هزینه مجدد بده (مقایسه ارزان).
            if (_lastOrientation != Screen.orientation || _lastSafeArea != Screen.safeArea)
                Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            min.x /= Screen.width;
            max.x /= Screen.width;
            min.y /= Screen.height;
            max.y /= Screen.height;

            if (!applyBottom)
                min.y = 0f;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

            _lastSafeArea = safe;
            _lastOrientation = Screen.orientation;
        }
    }
}
