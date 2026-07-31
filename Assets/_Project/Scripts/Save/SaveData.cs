using System;
using System.Collections.Generic;
using UnityEngine;

namespace FogWalker.Save
{
    /// <summary>
    /// مدل ریشه داده ذخیره؛ فقط فیلدهای public ساده (سازگار با JsonUtility و IL2CPP).
    /// قانون: برای تغییر ساختار، CurrentSchemaVersion را افزایش دهید و در SaveSystem.Migrate مدیریت کنید.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public SettingsData settings = new SettingsData();
        public ProgressData progress = new ProgressData();
        public StatsData stats = new StatsData();
    }

    /// <summary>تنظیمات کاربر؛ همگام با SettingsManager.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        [Range(0, 2)] public int qualityLevel = 1;        // 0=Performance, 1=Balanced, 2=High
        public int targetFps = 60;                        // فقط 30/45/60
        [Range(0f, 1f)] public float masterVolume = 0.9f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        [Range(0.1f, 3f)] public float cameraSensitivity = 1f;
        public bool invertY = false;
        public bool cameraShake = true;
        public bool haptics = true;
        [Range(0.7f, 1.4f)] public float controlScale = 1f;
        [Range(0.3f, 1f)] public float controlOpacity = 0.85f;
        public bool leftHanded = false;
        public bool autoQuality = false;                    // کیفیت‌پذیرسازی خودکار تطبیقی
        public string language = "fa";                    // "fa" | "en"
    }

    /// <summary>پیشرفت بازی؛ درجه سختی هم اینجا ذخیره می‌شود.</summary>
    [Serializable]
    public sealed class ProgressData
    {
        public int difficulty = 1;                        // 0=آسان 1=عادی 2=سخت
        public List<string> unlockedLevelIds = new List<string>();
        public string lastLevelId = string.Empty;
        public string lastCheckpointId = string.Empty;
        public string checkpointLevelId = string.Empty;
        public int lastObjectiveIndex = 0;
        public bool hasSave = false;                      // برای فعال‌بودن «ادامه بازی»
    }

    /// <summary>آمار تجمیعی بازیکن.</summary>
    [Serializable]
    public sealed class StatsData
    {
        public List<LevelStatRecord> levelRecords = new List<LevelStatRecord>();
        public int totalKills;
        public int totalDeaths;
    }

    /// <summary>رکورد آماری یک مرحله.</summary>
    [Serializable]
    public sealed class LevelStatRecord
    {
        public string levelId = string.Empty;
        public float bestTimeSeconds;
        [Range(0f, 1f)] public float bestAccuracy;
        public int bestKills;
        public int timesCompleted;
    }
}
