using System;
using UnityEngine;

namespace FogWalker.Core
{
    /// <summary>
    /// کاتالوگ داده‌محور صحنه‌ها؛ نام صحنه‌ها هرگز در کد Hardcode نمی‌شود.
    /// با FogWalker > Setup > 2 ساخته می‌شود. مراحل جدید فقط با افزودن یک Entry فعال می‌شوند.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "FogWalker/Scenes/Scene Catalog")]
    public sealed class SceneCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class LevelEntry
        {
            [Tooltip("شناسه یکتا و پایدار مرحله؛ در SaveData ذخیره می‌شود")] public string levelId = "level1";
            [Tooltip("نام دقیق صحنه در Build Settings")] public string sceneName = "Level1_Boulevard";
            [Tooltip("کلید متن نام مرحله در LocalizationTable")] public string displayNameKey = "level.1.name";
        }

        [Header("صحنه‌های ثابت")]
        public string bootstrapScene = "Bootstrap";
        public string mainMenuScene = "MainMenu";

        [Header("مراحل")]
        public LevelEntry[] levels = Array.Empty<LevelEntry>();

        /// <summary>اولین مرحله (برای شروع بازی جدید و فالبک‌ها).</summary>
        public LevelEntry GetFirstLevel()
        {
            return levels != null && levels.Length > 0 ? levels[0] : null;
        }

        /// <summary>یافتن مرحله با شناسه؛ اگر پیدا نشود null.</summary>
        public LevelEntry GetById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || levels == null)
                return null;
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] != null && levels[i].levelId == levelId)
                    return levels[i];
            return null;
        }
    }
}
