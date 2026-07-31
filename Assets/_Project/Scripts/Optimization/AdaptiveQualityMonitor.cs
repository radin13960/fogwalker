using FogWalker.Core;
using FogWalker.Settings;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Optimization
{
    /// <summary>
    /// کیفیت‌پذیرسازی پویا (Adaptive Quality): اگر فریم‌ریت برای چند ثانیه پیوسته زیر آستانه بود،
    /// Render Scale را پله‌ای پایین می‌آورد (تا ۰.۷ پروفایل) و در آرام‌شدن بازی برمی‌گرداند.
    /// فقط وقتی کاربر «کیفیت تطبیقی خودکار» را فعال کرده باشد؛ نوسان شدید ممنوع (پله‌های ثابت روان).
    /// </summary>
    public sealed class AdaptiveQualityMonitor : MonoBehaviour
    {
        [Header("پیکربندی")]
        [SerializeField] private float sampleWindow = 2.5f;
        [SerializeField] private float lowMarginFps = 8f;    // زیر این مقدار از هدف = تلقّی افت
        [SerializeField] private float upMarginFps = 2f;     // بالای این برای بازگشت
        [SerializeField] private float step = 0.1f;
        [SerializeField] private float cooldownBetweenSteps = 4f;

        private SettingsManager _settings;
        private QualityManager _quality;
        private float _sampleTimer;
        private float _accumulatedDelta;
        private int _frames;
        private float _cooldownTimer;

        private void Start()
        {
            ServiceLocator.TryGet(out _settings);
            ServiceLocator.TryGet(out _quality);
        }

        private void Update()
        {
            if (_settings == null || _settings.Data == null || !_settings.Data.autoQuality)
                return;
            if (_quality == null || !_quality.IsReady) return;
            // فقط در حالت Playing منطقی است
            if (ServiceLocator.TryGet(out GameStateManager state) && !state.IsGameplayActive)
                return;

            _accumulatedDelta += Time.unscaledDeltaTime;
            _frames++;
            _cooldownTimer -= Time.unscaledDeltaTime;
            _sampleTimer -= Time.unscaledDeltaTime;

            if (_sampleTimer > 0f) return;

            float avgDelta = _accumulatedDelta / Mathf.Max(1, _frames);
            float avgFps = 1f / Mathf.Max(0.0001f, avgDelta);
            _sampleTimer = sampleWindow;
            _accumulatedDelta = 0f;
            _frames = 0;

            if (_cooldownTimer > 0f) return;

            float target = _settings.Data.targetFps;
            float overrideNow = _quality.CurrentRenderScaleOverride;

            if (avgFps < target - lowMarginFps && overrideNow > 0.72f)
            {
                _quality.SetRenderScaleOverride(overrideNow - step);
                _cooldownTimer = cooldownBetweenSteps;
                GameLog.Info($"[Adaptive] افت FPS ({avgFps:0}) → RenderScale {overrideNow - step:0.00}");
            }
            else if (avgFps > target - upMarginFps && overrideNow < 1f)
            {
                _quality.SetRenderScaleOverride(overrideNow + step);
                _cooldownTimer = cooldownBetweenSteps;
                GameLog.Info($"[Adaptive] بازیابی FPS ({avgFps:0}) → RenderScale {overrideNow + step:0.00}");
            }
        }
    }
}
