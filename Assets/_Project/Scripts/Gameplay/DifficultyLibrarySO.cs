using UnityEngine;

namespace FogWalker.Gameplay
{
    /// <summary>
    /// کتابخانه درجه‌های سختی (آسان/عادی/سخت) برای دسترسی زمان‌اجرا.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyLibrary", menuName = "FogWalker/Difficulty/Difficulty Library")]
    public sealed class DifficultyLibrarySO : ScriptableObject
    {
        [Tooltip("به ترتیب: 0=آسان، 1=عادی، 2=سخت")]
        public DifficultySettingsSO[] difficulties = new DifficultySettingsSO[3];

        /// <summary>گرفتن تنظیمات سختی با ایندکس؛ خارج از محدوده → عادی.</summary>
        public DifficultySettingsSO Get(int index)
        {
            if (difficulties == null || difficulties.Length == 0) return null;
            index = Mathf.Clamp(index, 0, difficulties.Length - 1);
            return difficulties[index];
        }
    }

    /// <summary>
    /// نگهدارنده سختی فعال مرحله جاری؛ توسط GameplayBootstrapper از روی SaveData تنظیم می‌شود
    /// تا AI/Spawn بدون دسترسی مستقیم به Save کار کنند.
    /// </summary>
    public static class DifficultyContext
    {
        /// <summary>تنظیمات سختی فعال؛ قبل از شروع مرحله null است (کد مصرف‌کننده باید null-safe باشد).</summary>
        public static DifficultySettingsSO Current { get; set; }
    }
}
