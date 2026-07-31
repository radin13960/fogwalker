using System;
using FogWalker.Gameplay.Missions;
using UnityEngine;

namespace FogWalker.Audio
{
    /// <summary>ورودی کتابخانه افکت صوتی: کلید ← کلیپ‌های تصادفی + تنظیمات.</summary>
    [Serializable]
    public sealed class SfxEntry
    {
        public string key;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("حداقل فاصله زمانی بین دو پخش هم‌کلید (Voice Limiting)")] public float minInterval = 0.05f;
        [Tooltip("حداکثر تعداد هم‌زمان این صدا")] public int maxConcurrent = 3;
        [Range(0.9f, 1.1f)] public float pitchCenter = 1f;
        [Range(0f, 0.2f)] public float pitchVariation = 0.05f;
    }

    /// <summary>کتابخانه افکت‌های صوتی (داده‌محور، آفلاین؛ کلیپ‌ها بعداً اضافه می‌شوند).</summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "FogWalker/Audio/SFX Library")]
    public sealed class SfxLibrarySO : ScriptableObject
    {
        public SfxEntry[] entries = Array.Empty<SfxEntry>();

        /// <summary>یافتن ورودی با کلید؛ null اگر نیست.</summary>
        public SfxEntry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].key == key)
                    return entries[i];
            return null;
        }
    }

    /// <summary>ورودی موسیقی: مود ← کلیپ حلقه.</summary>
    [Serializable]
    public sealed class MusicEntry
    {
        public MusicMood mood = MusicMood.Exploration;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
    }

    /// <summary>کتابخانه موسیقی پویا (لایه‌ها: Exploration/Tension/Combat/Victory).</summary>
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "FogWalker/Audio/Music Library")]
    public sealed class MusicLibrarySO : ScriptableObject
    {
        public MusicEntry[] entries = Array.Empty<MusicEntry>();

        /// <summary>یافتن ورودی با مود؛ null اگر نیست.</summary>
        public MusicEntry Find(MusicMood mood)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].mood == mood)
                    return entries[i];
            return null;
        }
    }
}
