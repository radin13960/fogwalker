using FogWalker.Core;
using FogWalker.Utilities;
using TMPro;
using UnityEngine;

namespace FogWalker.Localization
{
    /// <summary>
    /// اتصال یک TMP_Text به کلید Localization؛ با تغییر زبان خودکار به‌روز می‌شود.
    /// در حالت اصلاحگر داخلی، متن فارسی Reshape و جهت آن دستی تنظیم می‌شود (isRightToLeftText خاموش می‌ماند
    /// چون ترتیب بصری را خودمان می‌سازیم). با RTLTMPro، useBuiltInRtlFix را خاموش کنید تا TMP مدیریت کند.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField, Tooltip("کلید متن در LocalizationTable")]
        private string key;

        [SerializeField, Tooltip("اگر روشن باشد و زبان fa باشد، متن با اصلاحگر داخلی آماده نمایش می‌شود.")]
        private bool applyRtlFix = true;

        private TMP_Text _label;
        private LocalizationManager _localization;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            TryHook();
            Refresh();
        }

        private void OnDisable()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= Refresh;
        }

        /// <summary>تغییر کلید در زمان اجرا (برای لیست‌های پویا).</summary>
        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        private void TryHook()
        {
            if (_localization != null) return;
            if (ServiceLocator.TryGet(out LocalizationManager loc))
            {
                _localization = loc;
                _localization.OnLanguageChanged += Refresh;
            }
        }

        private void Refresh()
        {
            if (_label == null) return;
            if (_localization == null)
            {
                TryHook();
                if (_localization == null) return; // قبل از بوت (مثلاً پیش‌نمایش Editor)
            }

            string text = _localization.GetText(key);
            bool isFa = _localization.CurrentLanguage == "fa";
            bool manualRtl = isFa && _localization.UseBuiltInRtlFix && applyRtlFix;

            if (manualRtl)
                text = PersianTextUtility.Fix(text);

            _label.text = text;
            // در حالت اصلاحگر دستی، TMP نباید دوباره ترتیب را دست‌کاری کند؛ در غیر این‌صورت (پلاگین واقعی) روشن.
            _label.isRightToLeftText = isFa && !manualRtl;
        }
    }
}
