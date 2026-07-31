using System;
using FogWalker.Localization;
using FogWalker.Optimization;
using FogWalker.Save;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace FogWalker.Settings
{
    /// <summary>
    /// تنظیمات کاربر را نگه می‌دارد، فوراً اعمال می‌کند (گرافیک/صدا/FPS/کنترل) و در Save می‌نویسد.
    /// ویجت‌های UI فقط Setterها را صدا می‌زنند؛ هیچ منطق اعمال داخل UI نیست.
    /// </summary>
    public sealed class SettingsManager : MonoBehaviour
    {
        // نام دقیق پارامترهای Exposed در AudioMixer (مستند ۰۲-۰۳).
        private const string MasterVolumeParam = "MasterVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";

        [Header("صدا")]
        [SerializeField, Tooltip("AudioMixer با گروه‌های Master/Music/SFX و پارامترهای Exposed هم‌نام ثابت‌ها")]
        private AudioMixer audioMixer;

        private ISaveSystem _save;
        private QualityManager _qualityManager;
        private LocalizationManager _localization;

        /// <summary>پس از اعمال هر تغییر (برای به‌روزرسانی UI/HUD).</summary>
        public event Action OnSettingsChanged;

        /// <summary>داده تنظیمات فعلی؛ قبل از Initialize ممکن است null باشد.</summary>
        public SettingsData Data => _save?.Data?.settings;

        /// <summary>اتصال وابستگی‌ها و اعمال کامل تنظیمات ذخیره‌شده. فقط از Bootstrapper فراخوانی شود.</summary>
        public void Initialize(ISaveSystem save, QualityManager qualityManager, LocalizationManager localization)
        {
            _save = save;
            _qualityManager = qualityManager;
            _localization = localization;
            ApplyAll();
        }

        /// <summary>خواندن مجدد از Save (مثلاً پس از ResetAll) و اعمال.</summary>
        public void ReloadFromSave() => ApplyAll();

        /// <summary>اعمال همه تنظیمات فعلی.</summary>
        public void ApplyAll()
        {
            if (Data == null) { GameLog.Warn("[Settings] داده تنظیمات موجود نیست."); return; }
            ApplyGraphics();
            ApplyAudio();
            OnSettingsChanged?.Invoke();
        }

        // ---------- گرافیک ----------

        /// <summary>سطح کیفیت: 0=Performance، 1=Balanced، 2=High.</summary>
        public void SetQualityLevel(int level)
        {
            Data.qualityLevel = Mathf.Clamp(level, 0, 2);
            _qualityManager?.ApplyProfile(Data.qualityLevel);
            Persist();
        }

        /// <summary>FPS هدف؛ به نزدیک‌ترین مقدار مجاز 30/45/60 گرد می‌شود.</summary>
        public void SetTargetFps(int fps)
        {
            Data.targetFps = SnapFps(fps);
            ApplyFrameRate();
            Persist();
        }

        // ---------- صدا ----------

        /// <summary>حجم صدای کلی (0..1 خطی؛ تبدیل dB داخلی).</summary>
        public void SetMasterVolume(float v) { Data.masterVolume = Clamp01(v); ApplyAudio(); Persist(); }
        /// <summary>حجم موسیقی (0..1).</summary>
        public void SetMusicVolume(float v) { Data.musicVolume = Clamp01(v); ApplyAudio(); Persist(); }
        /// <summary>حجم افکت‌ها (0..1).</summary>
        public void SetSfxVolume(float v) { Data.sfxVolume = Clamp01(v); ApplyAudio(); Persist(); }

        // ---------- دوربین و کنترل ----------

        /// <summary>حساسیت دوربین (0.1 تا 3).</summary>
        public void SetCameraSensitivity(float v) { Data.cameraSensitivity = Mathf.Clamp(v, 0.1f, 3f); Persist(); }
        /// <summary>معکوس‌کردن محور عمودی دوربین.</summary>
        public void SetInvertY(bool v) { Data.invertY = v; Persist(); }
        /// <summary>لرزش دوربین هنگام شلیک/انفجار.</summary>
        public void SetCameraShake(bool v) { Data.cameraShake = v; Persist(); }
        /// <summary>لرزش لمسی (Haptics) روی دستگاه‌های پشتیبانی‌کننده.</summary>
        public void SetHaptics(bool v) { Data.haptics = v; Persist(); }
        /// <summary>چیدمان چپ‌دست کنترل‌های لمسی.</summary>
        public void SetLeftHanded(bool v) { Data.leftHanded = v; Persist(); }
        /// <summary>مقیاس اندازه کنترل‌های لمسی (0.7 تا 1.4).</summary>
        public void SetControlScale(float v) { Data.controlScale = Mathf.Clamp(v, 0.7f, 1.4f); Persist(); }
        /// <summary>شفافیت کنترل‌های لمسی (0.3 تا 1).</summary>
        public void SetControlOpacity(float v) { Data.controlOpacity = Mathf.Clamp(v, 0.3f, 1f); Persist(); }

        /// <summary>کیفیت تطبیقی خودکار (Adaptive). وقتی روشن است، سیستم در افت فریم مداوم Render Scale را در محدوده امن پروفایل تنظیم می‌کند.</summary>
        public void SetAutoQuality(bool v) { Data.autoQuality = v; Persist(); }

        // ---------- زبان ----------

        /// <summary>تغییر زبان رابط ("fa" / "en").</summary>
        public void SetLanguage(string language)
        {
            Data.language = string.IsNullOrEmpty(language) ? "fa" : language;
            _localization?.SetLanguage(Data.language);
            Persist();
        }

        // ---------- داخلی ----------

        private void ApplyGraphics()
        {
            _qualityManager?.ApplyProfile(Data.qualityLevel);
            ApplyFrameRate();
        }

        private void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = 0; // کنترل FPS با targetFrameRate (استاندارد موبایل)
            Application.targetFrameRate = Data.targetFps;
        }

        private void ApplyAudio()
        {
            if (audioMixer == null)
            {
                GameLog.Warn("[Settings] AudioMixer اختصاص داده نشده؛ حجم صدا اعمال نمی‌شود.");
                return;
            }
            audioMixer.SetFloat(MasterVolumeParam, ToDecibel(Data.masterVolume));
            audioMixer.SetFloat(MusicVolumeParam, ToDecibel(Data.musicVolume));
            audioMixer.SetFloat(SfxVolumeParam, ToDecibel(Data.sfxVolume));
        }

        /// <summary>تبدیل خطی→دسی‌بل با سکوت کامل در صفر.</summary>
        private static float ToDecibel(float linear)
        {
            return linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
        }

        private static float Clamp01(float v) => Mathf.Clamp01(v);

        private static int SnapFps(int fps)
        {
            if (fps <= 37) return 30;
            if (fps <= 52) return 45;
            return 60;
        }

        private void Persist()
        {
            _save?.Save();
            OnSettingsChanged?.Invoke();
        }
    }
}
