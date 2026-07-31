using UnityEngine;

namespace FogWalker.Gameplay.AI
{
    /// <summary>انواع دشمن.</summary>
    public enum EnemyArchetype { Rifleman, Rusher, Heavy }

    /// <summary>
    /// داده آرکی‌تایپ دشمن (داده‌محور). مقادیر پایه؛ ضرایب سختی از DifficultyContext ضرب می‌شود.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyArchetype", menuName = "FogWalker/AI/Enemy Archetype")]
    public sealed class EnemyArchetypeDataSO : ScriptableObject
    {
        [Header("هویت")]
        public EnemyArchetype archetype = EnemyArchetype.Rifleman;
        [Tooltip("پری‌فب بصری/کپسول (Placeholder مجاز)")] public GameObject visualPrefab;

        [Header("سلامت")]
        public float health = 60f;

        [Header("حرکت")]
        public float walkSpeed = 2.4f;
        public float runSpeed = 4.6f;
        [Tooltip("ترجیح فاصله کمینه/بیشینه از بازیکن (متر)")] public Vector2 preferredRange = new Vector2(10f, 20f);

        [Header("ادراک")]
        public float viewDistance = 25f;
        [Range(30f, 180f)] public float fieldOfViewAngle = 110f;
        [Tooltip("زمان پر شدن آگاهی وقتی بازیکن در دید (ثانیه)")] public float awarenessFillTime = 0.9f;
        public float hearingRadiusMultiplier = 1f;

        [Header("آتش")]
        public float damagePerShot = 7f;
        public float roundsPerMinute = 240f;
        [Range(0f, 1f), Tooltip("دقت پایه آرکی‌تایپ (پیش از سختی)")] public float baseAccuracy = 0.55f;
        public int burstMin = 2;
        public int burstMax = 4;
        public float burstPause = 0.9f;
        [Tooltip("زمان تأخیر اولین واکنش پس از Alert (ثانیه)")] public float reactionTime = 0.5f;

        [Header("رفتار")]
        [Range(0f, 1f)] public float coverPreference = 0.7f;      // احتمال ترجیح کاور به PreCharge
        [Range(0f, 1f)] public float flankChance = 0.15f;          // احتمال سرکشی مسیر فرعی
        [Range(0f, 1f), Tooltip("در کمتر از این نسبت HP عقب‌نشینی کند؛ ۰=هرگز")] public float retreatBelowHealth = 0.25f;
        public bool canUseCover = true;
    }
}
