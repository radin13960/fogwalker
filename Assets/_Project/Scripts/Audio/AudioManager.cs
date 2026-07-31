using System.Collections.Generic;
using FogWalker.Core;
using FogWalker.Gameplay.Missions;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace FogWalker.Audio
{
    /// <summary>
    /// مدیریت صدا: منبع‌های پخش Pool‌شده برای SFX سه‌بعدی/دو بعدی، Voice Limiting بر اساس کلید،
    /// و لایه موسیقی دو-کاناله با Crossfade نرم بین مودها (Exploration/Tension/Combat/Victory).
    /// ایمن در نبود کلیپ/کتابخانه (هیچ‌وقت کرش نمی‌کند — Placeholder-friendly).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("کتابخانه‌ها")]
        [SerializeField] private SfxLibrarySO sfxLibrary;
        [SerializeField] private MusicLibrarySO musicLibrary;

        [Header("گروه‌های Mixer")]
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;

        [Header("منابع")]
        [SerializeField, Tooltip("تعداد AudioSource برای SFX هم‌زمان")]
        private int sfxVoiceCount = 10;

        private class Voice { public AudioSource Source; public string Key; public float StartTime; }
        private readonly List<Voice> _voices = new List<Voice>(12);
        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>(32);

        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool _musicAIsCurrent = true;
        private MusicMood _currentMood = MusicMood.Exploration;

        private static AudioManager _instance;
        private float _crossfadeTimer;
        private const float CrossfadeDuration = 1.6f;

        private void Awake()
        {
            _instance = this;

            // ساخت صداهای SFX
            var sfxHost = new GameObject("SFX_Voices").transform;
            sfxHost.SetParent(transform, false);
            for (int i = 0; i < sfxVoiceCount; i++)
            {
                var go = new GameObject("Voice_" + i);
                go.transform.SetParent(sfxHost, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f; // سه‌بعدی برای جهان
                src.rolloffMode = AudioRolloffMode.Linear;
                src.maxDistance = 35f;
                if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
                _voices.Add(new Voice { Source = src });
            }

            // دو کانال موسیقی برای Crossfade
            _musicA = CreateMusicSource("Music_A");
            _musicB = CreateMusicSource("Music_B");
        }

        private AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f; // دوبعدی
            if (musicGroup != null) src.outputAudioMixerGroup = musicGroup;
            return src;
        }

        // ---------- SFX ----------

        /// <summary>پخش افکت در موقعیت (سه‌بعدی) یا مرکز (null موقعیت = دوبعدی).</summary>
        public void PlaySfx(string key, Vector3? position = null, float volumeMultiplier = 1f)
        {
            if (sfxLibrary == null) return;
            SfxEntry entry = sfxLibrary.Find(key);
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

            // Voice Limiting: حداقل فاصله زمانی + سقف هم‌زمانی
            if (_lastPlayTime.TryGetValue(key, out float lastT) && Time.time - lastT < entry.minInterval)
                return;
            int concurrent = CountVoices(key);
            if (concurrent >= entry.maxConcurrent) return;

            Voice voice = GetFreeVoice();
            if (voice == null) return;

            AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;

            voice.Key = key;
            voice.StartTime = Time.time;
            voice.Source.clip = clip;
            voice.Source.volume = entry.volume * volumeMultiplier;
            voice.Source.pitch = entry.pitchCenter + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            voice.Source.spatialBlend = position.HasValue ? 1f : 0f;
            if (position.HasValue) voice.Source.transform.position = position.Value;
            voice.Source.Play();
            _lastPlayTime[key] = Time.time;
        }

        private Voice GetFreeVoice()
        {
            for (int i = 0; i < _voices.Count; i++)
                if (!_voices[i].Source.isPlaying) return _voices[i];
            // همه مشغول: قدیمی‌ترین را بازیافت کن
            Voice oldest = _voices[0];
            for (int i = 1; i < _voices.Count; i++)
                if (_voices[i].StartTime < oldest.StartTime) oldest = _voices[i];
            oldest.Source.Stop();
            return oldest;
        }

        private int CountVoices(string key)
        {
            int c = 0;
            for (int i = 0; i < _voices.Count; i++)
                if (_voices[i].Source.isPlaying && _voices[i].Key == key) c++;
            return c;
        }

        // ---------- موسیقی ----------

        /// <summary>تغییر مود موسیقی با Crossfade نرم (بدون قطع ناگهانی).</summary>
        public void SetMood(MusicMood mood)
        {
            if (mood == _currentMood) return;
            _currentMood = mood;

            if (musicLibrary == null) return;
            MusicEntry entry = musicLibrary.Find(mood);

            AudioSource next = _musicAIsCurrent ? _musicB : _musicA;
            AudioSource current = _musicAIsCurrent ? _musicA : _musicB;

            if (entry == null || entry.clip == null)
            {
                // بدون کلیپ: محو کانال فعلی (سکوت) — هیچ خطایی نمی‌دهد
                next.clip = null;
            }
            else
            {
                next.clip = entry.clip;
                next.volume = entry.volume;
                if (!next.isPlaying) next.Play();
            }

            _crossfadeTimer = CrossfadeDuration;
            _musicAIsCurrent = !_musicAIsCurrent;
        }

        private void Update()
        {
            if (_crossfadeTimer <= 0f) return;
            _crossfadeTimer -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(_crossfadeTimer / CrossfadeDuration);

            AudioSource fadeIn = _musicAIsCurrent ? _musicA : _musicB;
            AudioSource fadeOut = _musicAIsCurrent ? _musicB : _musicA;

            if (fadeIn.clip != null) fadeIn.volume = Mathf.Lerp(0f, fadeIn.volume > 0f ? fadeIn.volume : 0.7f, t);
            fadeOut.volume = Mathf.Lerp(fadeOut.volume, 0f, t);
            if (_crossfadeTimer <= 0f) fadeOut.Stop();
        }

        /// <summary>توقف همه (خروج از مرحله).</summary>
        public void StopAll()
        {
            foreach (var v in _voices) v.Source.Stop();
            _musicA.Stop(); _musicB.Stop();
        }

        // ---------- API استاتیک امن (مصرف در کد بدون لزوم Locator در مسیر داغ) ----------

        /// <summary>پخش SFX با کلید؛ اگر AudioManager آماده نباشد بی‌صدا رد می‌شود.</summary>
        public static void PlaySfxShielded(string key, Vector3 position, float volumeMultiplier = 1f)
        {
            if (_instance != null) _instance.PlaySfx(key, position, volumeMultiplier);
        }

        /// <summary>پخش SFX دوبعدی (UI).</summary>
        public static void PlaySfx2DShielded(string key, float volumeMultiplier = 1f)
        {
            if (_instance != null) _instance.PlaySfx(key, null, volumeMultiplier);
        }

        /// <summary>تغییر موسیقی اگر آماده باشد.</summary>
        public static void SetMoodShielded(MusicMood mood)
        {
            if (_instance != null) _instance.SetMood(mood);
        }
    }
}
