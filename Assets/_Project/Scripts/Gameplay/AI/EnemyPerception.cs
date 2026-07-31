using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Player;
using UnityEngine;

namespace FogWalker.Gameplay.AI
{
    /// <summary>
    /// ادراک دشمن: میدان دید مخروطی + Raycast خط دید (دید از پشت دیوار ممنوع) + شنوایی صداها.
    /// آگاهی 0..1 پر/خالی می‌شود؛ آخرین موقعیت شناخته‌شده بازیکن ذخیره می‌ماند.
    /// اسکن دوره‌ای (stagger شده) برای بودجه CPU وقتی چند دشمن فعال‌اند.
    /// </summary>
    public sealed class EnemyPerception : MonoBehaviour
    {
        [Header("سیم‌کشی")]
        [SerializeField, Tooltip("نقطه چشم (پیش‌فرض: +1.6 ارتفاع)")]
        private Transform eye;

        private EnemyArchetypeDataSO _data;
        private Transform _player;
        private float _scanTimer;
        private float _scanOffset;

        /// <summary>آگاهی فعلی (0..1)؛ ۱ یعنی Alert.</summary>
        public float Awareness { get; private set; }
        /// <summary>آیا الان بازیکن را می‌بیند؟</summary>
        public bool HasVisual { get; private set; }
        /// <summary>آخرین موقعیت شناخته‌شده بازیکن.</summary>
        public Vector3 LastKnownPlayerPosition { get; private set; }
        /// <summary>اعتبار آخرین موقعیت (دقیقه‌ای پیش ثبت شده؟)</summary>
        public float LastKnownAge { get; private set; }
        /// <summary>آیا صدای تازه شنیده؟ (مدت کوتاه)</summary>
        public bool HeardRecently { get; private set; }
        /// <summary>موقعیت آخرین صدای شنیده‌شده.</summary>
        public Vector3 LastHeardPosition { get; private set; }

        private float _hearTimer;

        /// <summary>پیکربندی با آرکی‌تایپ و هدف (بازیکن).</summary>
        public void Configure(EnemyArchetypeDataSO data, Transform playerTransform)
        {
            _data = data;
            _player = playerTransform;
            _scanOffset = Random.Range(0f, 0.2f);
        }

        private void OnEnable()
        {
            AISoundBus.OnSound += HandleSound;
        }

        private void OnDisable()
        {
            AISoundBus.OnSound -= HandleSound;
        }

        private void Update()
        {
            if (_data == null) return;

            LastKnownAge += Time.deltaTime;

            if (_hearTimer > 0f)
            {
                _hearTimer -= Time.deltaTime;
                if (_hearTimer <= 0f) HeardRecently = false;
            }

            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = 0.18f + _scanOffset; // stagger طبیعی بین دشمنان
                ScanVision();
            }
        }

        /// <summary>اسکن دید: فاصله، زاویه، خط دید.</summary>
        private void ScanVision()
        {
            HasVisual = false;
            if (_player == null) return;

            Vector3 eyePos = eye != null ? eye.position : transform.position + Vector3.up * 1.6f;
            Vector3 targetPos = _player.position + Vector3.up * 1.2f; // سینه

            float distance = Vector3.Distance(eyePos, targetPos);
            if (distance > _data.viewDistance) { DecayAwareness(); return; }

            Vector3 dirToTarget = (targetPos - eyePos).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > _data.fieldOfViewAngle * 0.5f) { DecayAwareness(); return; }

            // Raycast خط دید: فقط محیط می‌تواند حائل باشد
            if (Physics.Linecast(eyePos, targetPos, out RaycastHit hit, GameplayLayers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                DecayAwareness();
                return;
            }

            HasVisual = true;
            LastKnownPlayerPosition = _player.position;
            LastKnownAge = 0f;

            // نزدیک‌تر = سریع‌تر آگاه
            float proximityScale = Mathf.Lerp(2f, 0.6f, Mathf.Clamp01(distance / _data.viewDistance));
            Awareness = Mathf.Min(1f, Awareness + (0.18f * proximityScale) / Mathf.Max(0.2f, _data.awarenessFillTime));
        }

        private void DecayAwareness()
        {
            Awareness = Mathf.Max(0f, Awareness - 0.18f * 0.6f);
        }

        private void HandleSound(Vector3 position, float radius, float loudness)
        {
            if (_data == null) return;
            float effectiveRadius = radius * _data.hearingRadiusMultiplier;
            float dist = Vector3.Distance(transform.position, position);
            if (dist > effectiveRadius) return;

            HeardRecently = true;
            _hearTimer = 3.5f;
            LastHeardPosition = position;

            // صدای خیلی نزدیک/تند مستقیماً موقعیت بازیکن را لو می‌دهد
            if (loudness >= 0.99f && dist < effectiveRadius * 0.5f)
            {
                LastKnownPlayerPosition = position;
                LastKnownAge = 0f;
                Awareness = Mathf.Min(1f, Awareness + 0.5f);
            }
            else
            {
                Awareness = Mathf.Min(1f, Awareness + 0.25f);
            }
        }

        /// <summary>ریست کامل برای Pool.</summary>
        public void ResetPerception()
        {
            Awareness = 0f;
            HasVisual = false;
            HeardRecently = false;
            LastKnownAge = float.MaxValue;
        }
    }
}
