using System;
using System.Collections.Generic;
using FogWalker.Save;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Localization
{
    /// <summary>
    /// مدیریت زبان و متن‌های بازی. متن‌ها از LocalizationTableSO خوانده می‌شوند.
    /// تا قبل از نصب RTLTMPro، اصلاح شکل‌حروف فارسی با PersianTextUtility داخلی انجام می‌شود (useBuiltInRtlFix).
    /// </summary>
    public sealed class LocalizationManager : MonoBehaviour
    {
        [Header("منابع")]
        [SerializeField, Tooltip("فهرست جدول‌های متنی (LocTable_FA و...)")]
        private List<LocalizationTableSO> tables = new List<LocalizationTableSO>();

        [Header("RTL")]
        [SerializeField, Tooltip("اصلاحگر داخلی فارسی. پس از نصب RTLTMPro خاموش کنید.")]
        private bool useBuiltInRtlFix = true;

        private readonly HashSet<string> _missingWarned = new HashSet<string>();

        /// <summary>زبان فعلی ("fa" پیش‌فرض / "en").</summary>
        public string CurrentLanguage { get; private set; } = "fa";

        /// <summary>آیا اصلاحگر داخلی فعال است؟</summary>
        public bool UseBuiltInRtlFix => useBuiltInRtlFix;

        /// <summary>پس از تغییر زبان؛ LocalizedTextها خودشان را به‌روز می‌کنند.</summary>
        public event Action OnLanguageChanged;

        /// <summary>مقداردهی اولیه از روی Save (فقط Bootstrapper).</summary>
        public void Initialize(ISaveSystem save)
        {
            string lang = save?.Data?.settings?.language;
            SetLanguage(string.IsNullOrEmpty(lang) ? "fa" : lang, silent: true);
        }

        /// <summary>تغییر زبان فعال و اطلاع به مشترکین.</summary>
        public void SetLanguage(string language, bool silent = false)
        {
            language = language == "en" ? "en" : "fa";
            if (CurrentLanguage == language && !silent) return;
            CurrentLanguage = language;
            if (!silent) OnLanguageChanged?.Invoke();
        }

        /// <summary>دریافت متن با کلید؛ اگر پیدا نشود "#key" + هشدار یک‌باره در لاگ توسعه.</summary>
        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            for (int i = 0; i < tables.Count; i++)
            {
                LocalizationTableSO table = tables[i];
                if (table == null) continue;
                string value = table.Get(key, CurrentLanguage);
                if (!string.IsNullOrEmpty(value)) return value;
            }

            if (_missingWarned.Add(key))
                GameLog.Warn($"[Loc] کلید '{key}' در جدول‌ها یافت نشد.");
            return "#" + key;
        }
    }
}
