using System;
using System.Collections.Generic;

namespace FogWalker.Localization
{
    /// <summary>یک ورودی متن دوزبانه.</summary>
    [Serializable]
    public sealed class LocalizationEntry
    {
        public string key;
        public string fa;
        public string en;
    }

    /// <summary>
    /// جدول متن‌های بازی (SO)؛ هیچ متنی در کد Hardcode نمی‌شود.
    /// افزودن زبان جدید = افزودن فیلد به LocalizationEntry و تکمیل Get.
    /// </summary>
    [CreateAssetMenu(fileName = "LocTable", menuName = "FogWalker/Localization/Localization Table")]
    public sealed class LocalizationTableSO : UnityEngine.ScriptableObject
    {
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();

        private Dictionary<string, LocalizationEntry> _lookup;

        /// <summary>ساخت نگاشت داخلی (تنبل؛ بعد از تغییر در Editor دوباره ساخته می‌شود).</summary>
        public void BuildLookup()
        {
            _lookup = new Dictionary<string, LocalizationEntry>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                _lookup[entry.key] = entry;
            }
        }

        /// <summary>متن کلید به زبان داده‌شده؛ نبود کلید → null.</summary>
        public string Get(string key, string language)
        {
            if (_lookup == null) BuildLookup();
            if (string.IsNullOrEmpty(key)) return null;
            if (!_lookup.TryGetValue(key, out LocalizationEntry entry)) return null;
            return language == "en" ? Fallback(entry.en, entry.fa) : Fallback(entry.fa, entry.en);
        }

        private static string Fallback(string primary, string secondary)
        {
            return string.IsNullOrEmpty(primary) ? secondary : primary;
        }

#if UNITY_EDITOR
        private void OnValidate() => _lookup = null; // بازسازی کش پس از ویرایش در Inspector
#endif
    }
}
