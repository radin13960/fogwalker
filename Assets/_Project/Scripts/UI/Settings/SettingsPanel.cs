using System.Collections.Generic;
using FogWalker.Core;
using FogWalker.Localization;
using FogWalker.Save;
using FogWalker.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.UI.Settings
{
    /// <summary>
    /// پنل تنظیمات: گرافیک، FPS، صدا، دوربین، کنترل‌های لمسی، چپ‌دست، بازنشانی Save (با تأیید).
    /// همه تغییرات فوراً از طریق SettingsManager اعمال و ذخیره می‌شوند.
    /// ویجت‌ها فقط در صورت سیم‌شدن فعال‌اند (null-safe) تا نسخه‌های میانی UI بلوک نشوند.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [Header("گرافیک")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown fpsDropdown;

        [Header("صدا")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("دوربین")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Toggle cameraShakeToggle;

        [Header("کنترل لمسی")]
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Toggle leftHandedToggle;
        [SerializeField] private Slider controlScaleSlider;
        [SerializeField] private Slider controlOpacitySlider;
        [SerializeField, Tooltip("کیفیت تطبیقی خودکار")] private Toggle autoQualityToggle;

        [Header("بازنشانی پیشرفت")]
        [SerializeField] private Button resetSaveButton;
        [SerializeField] private GameObject confirmResetRoot;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("بازگشت")]
        [SerializeField] private Button backButton;

        private static readonly int[] FpsSteps = { 30, 45, 60 };
        private static readonly string[] QualityOptionKeys = { "settings.quality.performance", "settings.quality.balanced", "settings.quality.high" };

        private SettingsManager _settings;
        private ISaveSystem _save;
        private LocalizationManager _localization;
        private bool _listenersBound;

        /// <summary>بازکردن پنل و همگام‌سازی ویجت‌ها با داده فعلی.</summary>
        public void Open()
        {
            if (!ResolveServices()) return;
            gameObject.SetActive(true);
            BindListenersOnce();
            PopulateStaticDropdowns();
            SyncWidgets();
            if (confirmResetRoot != null) confirmResetRoot.SetActive(false);
        }

        /// <summary>بستن پنل.</summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private bool ResolveServices()
        {
            if (_settings == null) ServiceLocator.TryGet(out _settings);
            if (_save == null) ServiceLocator.TryGet(out _save);
            ServiceLocator.TryGet(out _localization);

            if (_settings != null && _save != null) return true;
            GameLog.Error("[SettingsPanel] SettingsManager/SaveSystem در دسترس نیست؛ از Bootstrap اجرا کنید.");
            return false;
        }

        // ---------- اتصال رویدادها ----------

        private void BindListenersOnce()
        {
            if (_listenersBound) return;
            _listenersBound = true;

            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(i => _settings.SetQualityLevel(i));
            if (fpsDropdown != null) fpsDropdown.onValueChanged.AddListener(i => _settings.SetTargetFps(FpsSteps[Mathf.Clamp(i, 0, FpsSteps.Length - 1)]));

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(v => _settings.SetMasterVolume(v));
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(v => _settings.SetMusicVolume(v));
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(v => _settings.SetSfxVolume(v));

            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(v => _settings.SetCameraSensitivity(v));
            if (invertYToggle != null) invertYToggle.onValueChanged.AddListener(v => _settings.SetInvertY(v));
            if (cameraShakeToggle != null) cameraShakeToggle.onValueChanged.AddListener(v => _settings.SetCameraShake(v));

            if (hapticsToggle != null) hapticsToggle.onValueChanged.AddListener(v => _settings.SetHaptics(v));
            if (leftHandedToggle != null) leftHandedToggle.onValueChanged.AddListener(v => _settings.SetLeftHanded(v));
            if (controlScaleSlider != null) controlScaleSlider.onValueChanged.AddListener(v => _settings.SetControlScale(v));
            if (controlOpacitySlider != null) controlOpacitySlider.onValueChanged.AddListener(v => _settings.SetControlOpacity(v));
            if (autoQualityToggle != null) autoQualityToggle.onValueChanged.AddListener(v => _settings.SetAutoQuality(v));

            if (resetSaveButton != null) resetSaveButton.onClick.AddListener(ShowResetConfirm);
            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ConfirmReset);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(HideResetConfirm);
            if (backButton != null) backButton.onClick.AddListener(Close);
        }

        /// <summary>ساخت گزینه‌های Dropdown به زبان فعلی (هر بار بازشدن تازه می‌شود).</summary>
        private void PopulateStaticDropdowns()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.options.Clear();
                foreach (string key in QualityOptionKeys)
                    qualityDropdown.options.Add(new TMP_Dropdown.OptionData(Fixed(key)));
                qualityDropdown.RefreshShownValue();
            }

            if (fpsDropdown != null)
            {
                fpsDropdown.options.Clear();
                foreach (int fps in FpsSteps)
                    fpsDropdown.options.Add(new TMP_Dropdown.OptionData(PersianDigitsIfFa(fps.ToString()) + " FPS"));
                fpsDropdown.RefreshShownValue();
            }
        }

        // ---------- همگام‌سازی بدون شلیک رویداد ----------

        private void SyncWidgets()
        {
            SettingsData data = _settings.Data;
            if (data == null) return;

            if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(data.qualityLevel);
            if (fpsDropdown != null) fpsDropdown.SetValueWithoutNotify(FpsIndex(data.targetFps));

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(data.masterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(data.musicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(data.sfxVolume);

            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(data.cameraSensitivity);
            if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(data.invertY);
            if (cameraShakeToggle != null) cameraShakeToggle.SetIsOnWithoutNotify(data.cameraShake);

            if (hapticsToggle != null) hapticsToggle.SetIsOnWithoutNotify(data.haptics);
            if (leftHandedToggle != null) leftHandedToggle.SetIsOnWithoutNotify(data.leftHanded);
            if (controlScaleSlider != null) controlScaleSlider.SetValueWithoutNotify(data.controlScale);
            if (controlOpacitySlider != null) controlOpacitySlider.SetValueWithoutNotify(data.controlOpacity);
            if (autoQualityToggle != null) autoQualityToggle.SetIsOnWithoutNotify(data.autoQuality);
        }

        private static int FpsIndex(int fps)
        {
            for (int i = 0; i < FpsSteps.Length; i++)
                if (FpsSteps[i] == fps) return i;
            return FpsSteps.Length - 1; // پیش‌فرض 60
        }

        // ---------- بازنشانی ----------

        private void ShowResetConfirm()
        {
            if (confirmResetRoot != null) confirmResetRoot.SetActive(true);
        }

        private void HideResetConfirm()
        {
            if (confirmResetRoot != null) confirmResetRoot.SetActive(false);
        }

        private void ConfirmReset()
        {
            _save.ResetAll();
            _settings.ReloadFromSave();
            HideResetConfirm();
            SyncWidgets();
            GameLog.Info("[SettingsPanel] پیشرفت و تنظیمات بازنشانی شد.");
        }

        // ---------- کمک‌کارهای متن ----------

        private string Fixed(string key)
        {
            if (_localization == null) return key;
            string text = _localization.GetText(key);
            return _localization.UseBuiltInRtlFix && _localization.CurrentLanguage == "fa"
                ? PersianTextUtility.Fix(text)
                : text;
        }

        private string PersianDigitsIfFa(string latin)
        {
            return _localization != null && _localization.CurrentLanguage == "fa"
                ? PersianTextUtility.ToPersianDigits(latin)
                : latin;
        }
    }
}
