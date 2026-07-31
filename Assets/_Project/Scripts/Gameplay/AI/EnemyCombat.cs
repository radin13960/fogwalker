using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Player;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.AI
{
    /// <summary>
    /// آتش دشمن با مدل خطاپذیر و منصفانه: دقت = آرکی‌تایپ × سختی × فاصله؛
    /// برست‌های محدود با مکث؛ بررسی LOS واقعی برای عدم شلیک از پشت دیوار؛
    /// شلیک با Ray از Muzzle به سمت سینه بازیکن با آفست خطا.
    /// </summary>
    public sealed class EnemyCombat : MonoBehaviour
    {
        [Header("سیم‌کشی")]
        [SerializeField] private Transform muzzle;

        private EnemyArchetypeDataSO _data;
        private Transform _player;
        private HealthComponent _playerHealth;

        private int _burstRemaining;
        private float _shotTimer;
        private float _pauseTimer;
        private float _reactionTimer;
        private bool _reactionElapsed;

        /// <summary>آیا آماده شلیک است (واکنش تمام شده)؟</summary>
        public bool IsEngaged => _reactionElapsed;

        /// <summary>پیکربندی.</summary>
        public void Configure(EnemyArchetypeDataSO data, Transform playerTransform)
        {
            _data = data;
            _player = playerTransform;
            _playerHealth = playerTransform != null ? playerTransform.GetComponentInParent<HealthComponent>() : null;
        }

        private void Awake()
        {
            if (muzzle == null)
            {
                var mz = new GameObject("Muzzle").transform;
                mz.SetParent(transform, false);
                mz.localPosition = new Vector3(0.15f, 1.35f, 0.4f);
                muzzle = mz;
            }
        }

        /// <summary>شروع درگیری: تایمر واکنش (با سختی مقیاس می‌شود).</summary>
        public void BeginEngagement()
        {
            float scale = DifficultyContext.Current != null ? DifficultyContext.Current.enemyReactionScale : 1f;
            _reactionTimer = _data.reactionTime / Mathf.Max(0.2f, scale);
            _reactionElapsed = false;
        }

        /// <summary>پایان درگیری.</summary>
        public void EndEngagement()
        {
            _reactionElapsed = false;
            _burstRemaining = 0;
        }

        private void Update()
        {
            if (_data == null) return;

            if (!_reactionElapsed)
            {
                _reactionTimer -= Time.deltaTime;
                if (_reactionTimer <= 0f) _reactionElapsed = true;
                return;
            }

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer > 0f) return;
                // شروع برست جدید
                _burstRemaining = Random.Range(_data.burstMin, _data.burstMax + 1);
            }

            if (_shotTimer > 0f) _shotTimer -= Time.deltaTime;
        }

        /// <summary>تلاش برای شلیک تکی اگر برست فعال است؛ true یعنی شلیک انجام شد.</summary>
        public bool TryShootOnce()
        {
            if (!_reactionElapsed || _data == null || _player == null) return false;
            if (_pauseTimer > 0f || _shotTimer > 0f || _burstRemaining <= 0) return false;

            _shotTimer = 60f / Mathf.Max(30f, _data.roundsPerMinute);
            _burstRemaining--;

            FireRay();

            if (_burstRemaining <= 0)
                _pauseTimer = _data.burstPause * Random.Range(0.85f, 1.3f);

            return true;
        }

        private void FireRay()
        {
            if (muzzle == null || _player == null) return;

            Vector3 targetPos = _player.position + Vector3.up * 1.25f;
            Vector3 dir = (targetPos - muzzle.position).normalized;
            float distance = Vector3.Distance(muzzle.position, targetPos);

            // دقت مؤثر: پایه × سختی × افت فاصله
            float diffAcc = DifficultyContext.Current != null ? DifficultyContext.Current.enemyBaseAccuracy : 0.55f;
            float accuracy = _data.baseAccuracy * diffAcc * Mathf.Lerp(1.15f, 0.55f, Mathf.Clamp01(distance / _data.viewDistance));
            accuracy = Mathf.Clamp01(accuracy);

            // خطای زاویه‌ای: دقت کمتر = پراکندگی بیشتر
            float missConeDeg = Mathf.Lerp(9f, 1.2f, accuracy);
            Vector3 shootDir = Weapons.WeaponMath.ApplyConeSpread(dir, missConeDeg);

            // ۱) آیا بازیکن در خط دید است؟ (جلوگیری از تیر از پشت دیوار)
            if (Physics.Linecast(muzzle.position, targetPos, out RaycastHit block, GameplayLayers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                // خط بسته؛ احتمال اصابت به حائل محیط
                SpawnTracer(muzzle.position, block.point);
                AISoundBus.Report(muzzle.position, 20f, 0.8f);
                return;
            }

            Vector3 endPoint;
            if (Physics.Raycast(muzzle.position, shootDir, out RaycastHit hit, distance + 2f, GameplayLayers.BulletMask | GameplayLayers.HitboxMask | (1 << GameplayLayers.Player), QueryTriggerInteraction.Ignore))
                endPoint = hit.point;
            else
                endPoint = muzzle.position + shootDir * distance;

            SpawnTracer(muzzle.position, endPoint);
            AISoundBus.Report(muzzle.position, 20f, 0.8f);
            Audio.AudioManager.PlaySfxShielded("sfx.enemy.fire", transform.position);

            // آیا تیر به Hitbox بازیکن خورد؟
            if (hit.collider != null)
            {
                var hitbox = hit.collider.GetComponentInParent<Hitbox>();
                var playerHc = hit.collider.GetComponentInParent<PlayerController>();
                if (hitbox != null && hitbox.Owner != null && hitbox.Owner.GetComponent<PlayerController>() != null)
                {
                    float diffDmg = DifficultyContext.Current != null ? DifficultyContext.Current.enemyDamageMultiplier : 1f;
                    hitbox.TakeDamage(new DamageInfo
                    {
                        Amount = _data.damagePerShot * diffDmg,
                        Type = DamageType.Bullet,
                        HitPoint = endPoint,
                        Direction = shootDir,
                        Instigator = this,
                    });
                }
            }
        }

        private void SpawnTracer(Vector3 from, Vector3 to)
        {
            // در این نسخه Tracer دشمن حذف شده برای بودجه — در آینده از Pool
        }

        /// <summary>ریست برای Pool.</summary>
        public void ResetCombat()
        {
            _burstRemaining = 0;
            _shotTimer = 0f;
            _pauseTimer = 0f;
            _reactionElapsed = false;
        }
    }
}
