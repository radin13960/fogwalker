using System;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FogWalker.Optimization
{
    /// <summary>
    /// اعمال پروفایل‌های کیفیت روی URP و QualitySettings.
    /// برای جلوگیری از تغییر Asset اصلی در Editor، یک نسخه Runtime از URP Asset ساخته و روی آن می‌نویسد.
    /// نکته پروژه: فقط یک سطح Quality در Project Settings داریم؛ تنوع کیفیت از همین‌جا کنترل می‌شود.
    /// </summary>
    public sealed class QualityManager : MonoBehaviour
    {
        [Header("پروفایل‌ها (Setup > 2)")]
        [SerializeField] private QualityProfileSO performanceProfile;
        [SerializeField] private QualityProfileSO balancedProfile;
        [SerializeField] private QualityProfileSO highProfile;

        private UniversalRenderPipelineAsset _runtimeUrpAsset;

        /// <summary>سطح فعال فعلی (-1 تا قبل از اولین اعمال).</summary>
        public int CurrentProfileLevel { get; private set; } = -1;

        /// <summary>حداقل یک پروفایل با موفقیت اعمال شده؟</summary>
        public bool IsReady => CurrentProfileLevel >= 0;

        /// <summary>پس از اعمال موفق پروفایل (سطح).</summary>
        public event Action<int> OnQualityApplied;

        /// <summary>آیا پس‌پردازش در پروفایل فعلی فعال است؟ (دوربین‌ها در فازهای بعدی از این می‌خوانند)</summary>
        public bool PostProcessingEnabled { get; private set; } = true;

        private float _renderScaleBase = 1f;     // مقدار پروفایل (بدون اورراید)
        private float _renderScaleOverride = 1f; // ضریب Adaptive Quality

        /// <summary>
        /// اورراید موقت Render Scale توسط Adaptive Quality (۰.۷ تا ۱ ضرب در مقدار پروفایل).
        /// تنظیم کاربر همیشه مبناست؛ اورراید فقط کاهش می‌دهد و با تغییر پروفایل ریست می‌شود.
        /// </summary>
        public void SetRenderScaleOverride(float multiplier01)
        {
            _renderScaleOverride = Mathf.Clamp(multiplier01, 0.7f, 1f);
            if (_runtimeUrpAsset != null)
                _runtimeUrpAsset.renderScale = _renderScaleBase * _renderScaleOverride;
        }

        /// <summary>ضریب فعلی Adaptive (1 یعنی بدون اورراید).</summary>
        public float CurrentRenderScaleOverride => _renderScaleOverride;

        /// <summary>پروفایل متناظر با سطح؛ نامعتبر → Balanced.</summary>
        public QualityProfileSO GetProfile(int level)
        {
            switch (level)
            {
                case 0: return performanceProfile;
                case 2: return highProfile;
                default: return balancedProfile;
            }
        }

        /// <summary>اعمال کامل پروفایل سطح داده‌شده (0/1/2).</summary>
        public void ApplyProfile(int level)
        {
            QualityProfileSO profile = GetProfile(level);
            if (profile == null)
            {
                GameLog.Error($"[Quality] پروفایل سطح {level} به QualityManager اختصاص داده نشده است!");
                return;
            }

            UniversalRenderPipelineAsset source = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
#if UNITY_6000_0_OR_NEWER
            if (source == null)
                source = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
#else
            if (source == null)
                source = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
#endif

            if (source != null)
            {
                EnsureRuntimeCopy(source);
                ApplyToUrpAsset(profile);
            }
            else
            {
                GameLog.Warn("[Quality] URP Asset پیدا نشد؛ فقط تنظیمات کلی QualitySettings اعمال شد.");
            }

            QualitySettings.pixelLightCount = profile.pixelLightCount;
            QualitySettings.lodBias = profile.lodBias;
#if UNITY_2023_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = profile.textureMipmapLimit;
#else
            QualitySettings.masterTextureLimit = profile.textureMipmapLimit;
#endif

            PostProcessingEnabled = profile.postProcessing;
            CurrentProfileLevel = level;
            OnQualityApplied?.Invoke(level);
            GameLog.Info($"[Quality] پروفایل '{profile.profileName}' اعمال شد. (Scale={profile.renderScale}, Shadows={profile.shadowDistance}m)");
        }

        private void EnsureRuntimeCopy(UniversalRenderPipelineAsset source)
        {
            if (_runtimeUrpAsset != null) return;
            _runtimeUrpAsset = Instantiate(source);
            _runtimeUrpAsset.name = source.name + " (Runtime)";
            QualitySettings.renderPipeline = _runtimeUrpAsset;
        }

        private void ApplyToUrpAsset(QualityProfileSO profile)
        {
            _renderScaleBase = profile.renderScale;
            _renderScaleOverride = 1f; // تغییر پروفایل، اورراید تطبیقی را ریست می‌کند
            _runtimeUrpAsset.renderScale = profile.renderScale;
            _runtimeUrpAsset.msaaSampleCount = Mathf.Max(1, profile.msaaSampleCount);
            _runtimeUrpAsset.supportsHDR = profile.hdr;
            _runtimeUrpAsset.mainLightShadowmapResolution = profile.mainLightShadowResolution;
            _runtimeUrpAsset.shadowDistance = profile.shadowDistance;
            _runtimeUrpAsset.shadowCascadeCount = profile.shadowCascades;
        }
    }
}
