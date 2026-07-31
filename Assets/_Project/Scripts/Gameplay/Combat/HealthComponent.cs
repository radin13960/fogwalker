using System;
using FogWalker.Core;
using UnityEngine;

namespace FogWalker.Gameplay.Combat
{
    /// <summary>
    /// کامپوننت سلامت مشترک بازیکن و دشمن: آسیب، شفا، بازتولید محدود، مرگ.
    /// مقادیر از PlayerTuning/ArchetypeData تزریق می‌شود (Initialize) — بدون Magic Number.
    /// </summary>
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("پیکربندی (با Initialize تزریق می‌شود)")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField, Tooltip("بازتولید خودکار محدود؛ ۰ یعنی فقط با کیت درمانی")]
        private float regenPerSecond = 0f;
        [SerializeField] private float regenDelay = 3f;

        private float _lastDamageTime;
        private bool _initialized;

        /// <summary>سلامت فعلی.</summary>
        public float CurrentHealth { get; private set; }
        /// <summary>سلامت حداکثر.</summary>
        public float MaxHealth => maxHealth;
        /// <summary>زنده است؟</summary>
        public bool IsAlive => CurrentHealth > 0f;
        /// <summary>نسبت 0..1 برای نوار سلامت.</summary>
        public float Normalized => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;

        /// <summary>(اطلاعات ضربه، سلامت باقی‌مانده)</summary>
        public event Action<DamageInfo, float> OnDamaged;
        /// <summary>(آخرین ضربه)</summary>
        public event Action<DamageInfo> OnDied;
        /// <summary>(مقدار شفا، سلامت جدید)</summary>
        public event Action<float, float> OnHealed;

        private void Awake()
        {
            if (!_initialized)
                Initialize(maxHealth, regenPerSecond, regenDelay);
        }

        /// <summary>تزریق مقادیر از داده‌های بازی (Tuning/Archetype × سختی).</summary>
        public void Initialize(float max, float regenPerSec, float delay)
        {
            maxHealth = Mathf.Max(1f, max);
            regenPerSecond = Mathf.Max(0f, regenPerSec);
            regenDelay = Mathf.Max(0f, delay);
            CurrentHealth = maxHealth;
            _initialized = true;
            _lastDamageTime = float.NegativeInfinity;
        }

        private void Update()
        {
            // بازتولید خودکار محدود (اگر پیکربندی شده): فقط پس از تأخیر بدون آسیب.
            if (IsAlive && regenPerSecond > 0f && CurrentHealth < maxHealth &&
                Time.time - _lastDamageTime >= regenDelay)
            {
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenPerSecond * Time.deltaTime);
            }
        }

        /// <inheritdoc/>
        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || info.Amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - info.Amount);
            _lastDamageTime = Time.time;

            OnDamaged?.Invoke(info, CurrentHealth);
            DamageEvents.RaiseDamaged(this, info);

            if (!IsAlive)
                Die(info);
        }

        /// <summary>شفا با مقدار مشخص (کیت درمانی) — به مرز ماکزیمم محدود.</summary>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            float before = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            float healed = CurrentHealth - before;
            if (healed > 0f) OnHealed?.Invoke(healed, CurrentHealth);
            return healed;
        }

        /// <summary>ریست برای Spawn مجدد از Pool.</summary>
        public void Revive()
        {
            CurrentHealth = maxHealth;
            _lastDamageTime = float.NegativeInfinity;
        }

        private void Die(DamageInfo lastHit)
        {
            OnDied?.Invoke(lastHit);
            DamageEvents.RaiseDied(this, lastHit);
        }
    }
}
