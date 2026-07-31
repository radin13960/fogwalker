using UnityEngine;

namespace FogWalker.Gameplay.Combat
{
    /// <summary>
    /// ناحیه برخورد روی بدن (سر/تنه/اندام) با ضریب آسیب؛ آسیب را به HealthComponent والد می‌رساند.
    /// ضریب بالای ۱.۵ به‌عنوان Headshot علامت می‌خورد (برای آمار و بازخورد).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Hitbox : MonoBehaviour, IDamageable
    {
        [SerializeField, Tooltip("۱=تنه، ۲=سر")]
        private float damageMultiplier = 1f;

        private HealthComponent _owner;

        /// <summary>مالک سلامت.</summary>
        public HealthComponent Owner => _owner != null ? _owner : (_owner = GetComponentInParent<HealthComponent>());
        /// <summary>ضریب آسیب این ناحیه.</summary>
        public float Multiplier => damageMultiplier;
        /// <inheritdoc/>
        public bool IsAlive => Owner != null && Owner.IsAlive;

        private void Awake()
        {
            _owner = GetComponentInParent<HealthComponent>();
            // قانون لایه: روی روت بازیکن (CharacterController) لایه را تغییر نده تا گلوله خودی به خودمان نخورد.
            if (GetComponent<CharacterController>() == null)
                gameObject.layer = Core.GameplayLayers.Hitbox;
        }

        /// <inheritdoc/>
        public void TakeDamage(DamageInfo info)
        {
            if (Owner == null) return;
            if (damageMultiplier > 1.5f) info.IsHeadshot = true;
            info.Amount *= damageMultiplier;
            Owner.TakeDamage(info);
        }
    }
}
