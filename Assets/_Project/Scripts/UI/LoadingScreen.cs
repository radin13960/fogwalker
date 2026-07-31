using FogWalker.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.UI
{
    /// <summary>
    /// صفحه بارگذاری: نوار پیشرفت + درصد با ارقام فارسی + راهنما (LocalizedText).
    /// با CanvasGroup کنترل می‌شود؛ در شروع بازی کاملاً مخفی است و ورودی را بلاک می‌کند تا تپ ناخواسته رد نشود.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("CanvasGroup ریشه این صفحه")]
        private CanvasGroup group;

        [SerializeField, Tooltip("اسلایدر پیشرفت (0..1)")]
        private Slider progressBar;

        [SerializeField, Tooltip("متن درصد پیشرفت")]
        private TMP_Text percentText;

        private void Awake()
        {
            EnsureReferences();
            Hide();
        }

        /// <summary>نمایش صفحه و ریست پیشرفت.</summary>
        public void Show()
        {
            EnsureReferences();
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = true; // مسدودسازی تپ هنگام بارگذاری
            SetProgress(0f);
        }

        /// <summary>پنهان‌کردن صفحه.</summary>
        public void Hide()
        {
            EnsureReferences();
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        /// <summary>تنظیم پیشرفت 0..1؛ درصد با ارقام فارسی.</summary>
        public void SetProgress(float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);
            if (progressBar != null)
                progressBar.SetValueWithoutNotify(progress01);
            if (percentText != null)
                percentText.text = PersianTextUtility.ToPersianDigits(Mathf.RoundToInt(progress01 * 100f).ToString()) + "٪";
        }

        /// <summary>اگر سیم‌کشی Inspector ناقص بود، خودش از فرزندان پیدا کند (با هشدار توسعه).</summary>
        private void EnsureReferences()
        {
            if (group == null)
            {
                group = GetComponentInChildren<CanvasGroup>(true);
                if (group == null) GameLog.Warn("[LoadingScreen] CanvasGroup پیدا نشد.");
            }
            if (progressBar == null)
                progressBar = GetComponentInChildren<Slider>(true);
            if (percentText == null)
                percentText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
