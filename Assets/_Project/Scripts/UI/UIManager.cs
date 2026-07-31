using UnityEngine;

namespace FogWalker.UI
{
    /// <summary>
    /// مدیریت لایه‌های UI سراسری. فاز ۱: فقط صفحه Loading.
    /// در فاز ۲، HUD و PauseMenu با همین الگو (ارجاع SerializeField + متدهای عمومی ساده) اضافه می‌شوند.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("سراسری")]
        [SerializeField, Tooltip("صفحه بارگذاری (فرزند همین پری‌فب)")]
        private LoadingScreen loadingScreen;

        /// <summary>نمایش صفحه بارگذاری.</summary>
        public void ShowLoading()
        {
            if (loadingScreen != null) loadingScreen.Show();
        }

        /// <summary>پنهان‌کردن صفحه بارگذاری.</summary>
        public void HideLoading()
        {
            if (loadingScreen != null) loadingScreen.Hide();
        }

        /// <summary>به‌روزرسانی نوار پیشرفت بارگذاری (0..1).</summary>
        public void SetLoadingProgress(float progress01)
        {
            if (loadingScreen != null) loadingScreen.SetProgress(progress01);
        }
    }
}
