using System;
using UnityEngine;

namespace FogWalker.Gameplay.Combat
{
    /// <summary>انواع آسیب؛ برای گسترش‌های آینده (ضد زره، مقاومت و...).</summary>
    public enum DamageType { Bullet, Explosion, Melee, Environmental }

    /// <summary>بسته کامل اطلاعات یک ضربه.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public Vector3 HitPoint;
        public Vector3 Direction;
        public Component Instigator;     // چه کسی زد (برای نشانگر جهت حمله و آمار)
        public bool IsHeadshot;
    }

    /// <summary>هر چیز قابل آسیب (بازیکن، دشمن، جعبه تخریب‌پذیر).</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>
    /// گذرگاه رویدادهای مبارزه (کاهش وابستگی مستقیم سیستم‌ها).
    /// ثبت/لغو ثبت: در OnEnable/OnDisable. در آبجکت‌های Pool‌شده ریسک Leak را جدی بگیرید.
    /// </summary>
    public static class DamageEvents
    {
        /// <summary>(هدف، اطلاعات ضربه) — پس از اعمال آسیب.</summary>
        public static event Action<Component, DamageInfo> OnDamaged;
        /// <summary>(هدف، آخرین ضربه) — هنگام مرگ.</summary>
        public static event Action<Component, DamageInfo> OnDied;

        public static void RaiseDamaged(Component target, DamageInfo info) => OnDamaged?.Invoke(target, info);
        public static void RaiseDied(Component target, DamageInfo info) => OnDied?.Invoke(target, info);
    }

    /// <summary>
    /// گذرگاه صدای محیطی برای شنوایی AI: شلیک، انفجار، دویدن، نارنجک.
    /// </summary>
    public static class AISoundBus
    {
        /// <summary>(موقعیت، شعاع شنیده‌شدن، بلندی 0..1)</summary>
        public static event Action<Vector3, float, float> OnSound;

        public static void Report(Vector3 position, float radius, float loudness)
        {
            OnSound?.Invoke(position, radius, loudness);
        }
    }

    /// <summary>ابزار آسیب ناحیه‌ای انفجار با Falloff خطی از مرکز.</summary>
    public static class ExplosionUtility
    {
        private static readonly Collider[] Buffer = new Collider[32];

        /// <summary>
        /// آسیب ناحیه‌ای به همه HealthComponentهای شعاع (یک‌بار برای هر قربانی؛ با Falloff خطی).
        /// </summary>
        public static int DealAreaDamage(Vector3 center, float radius, float baseDamage, DamageType type, Component instigator, int layerMask)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, Buffer, layerMask, QueryTriggerInteraction.Ignore);
            int applied = 0;
            for (int i = 0; i < count; i++)
            {
                Collider col = Buffer[i];
                if (col == null) continue;
                var health = col.GetComponentInParent<HealthComponent>();
                if (health == null || !health.IsAlive) continue;

                // جلوگیری از چندبار آسیب به یک قربانی با چند کالایدر
                bool already = false;
                for (int j = 0; j < i; j++)
                    if (Buffer[j] != null && Buffer[j].GetComponentInParent<HealthComponent>() == health) { already = true; break; }
                if (already) continue;

                float dist = Vector3.Distance(center, col.ClosestPoint(center));
                float falloff = 1f - Mathf.Clamp01(dist / radius);
                float damage = baseDamage * Mathf.Lerp(0.25f, 1f, falloff); // حداقل ۲۵٪ در لبه

                health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Type = type,
                    HitPoint = col.ClosestPoint(center),
                    Direction = (health.transform.position - center).normalized,
                    Instigator = instigator,
                });
                applied++;
            }
            return applied;
        }
    }
}
