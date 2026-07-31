using UnityEngine;

namespace FogWalker.Optimization
{
    /// <summary>
    /// داده یک پروفایل کیفیت (Performance / Balanced / High).
    /// مقادیر اولیه با Setup ساخته می‌شوند و طبق جدول سند ۰۲ قابل بالانس هستند.
    /// </summary>
    [CreateAssetMenu(fileName = "QualityProfile", menuName = "FogWalker/Quality/Quality Profile")]
    public sealed class QualityProfileSO : ScriptableObject
    {
        [Header("هویت")]
        public string profileName = "Balanced";

        [Header("رندر")]
        [Range(0.5f, 1f), Tooltip("مقیاس رزولوشن رندر (UPSCALER ساده)")] public float renderScale = 0.85f;
        [Tooltip("نمونه MSAA: 1=خاموش، 2، 4")] public int msaaSampleCount = 2;
        public bool hdr = true;

        [Header("سایه")]
        public int mainLightShadowResolution = 2048;
        [Range(0f, 300f)] public float shadowDistance = 60f;
        [Range(0, 4)] public int shadowCascades = 2;

        [Header("جزئیات")]
        [Range(1, 8)] public int pixelLightCount = 3;
        [Range(0.3f, 2f)] public float lodBias = 1f;
        [Range(0, 3), Tooltip("0=کیفیت کامل؛ عدد بیشتر = تکسچر کم‌رزولوشن‌تر")] public int textureMipmapLimit = 0;
        [Tooltip("فعال‌بودن پس‌پردازش (با Volumeها در فازهای بعدی اعمال می‌شود)")] public bool postProcessing = true;
        [Tooltip("سقف ذرات هم‌زمان پیشنهادی برای این پروفایل")] public int particleBudget = 100;
        [Tooltip("برد دوربین (Far Clip) پیشنهادی این پروفایل")] public float drawDistance = 150f;
    }
}
