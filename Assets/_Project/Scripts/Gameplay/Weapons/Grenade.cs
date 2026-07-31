using System;
using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Optimization;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Weapons
{
    /// <summary>داده نارنجک: شعاع آسیب کنترل‌شده + فیوز + پرتاب.</summary>
    [CreateAssetMenu(fileName = "GrenadeData", menuName = "FogWalker/Weapons/Grenade Data")]
    public sealed class GrenadeDataSO : ScriptableObject
    {
        public float damage = 90f;
        public float radius = 4.5f;
        public float fuseSeconds = 2.2f;
        public float throwSpeed = 12f;
        [Tooltip("بی‌ضریب رو به بالای پرتاب")] public float upForce = 3.5f;
        public float throwCooldown = 1.2f;
        public GameObject projectilePrefab;
        public GameObject explosionFxPrefab;
        public string explosionSfxKey = "sfx.explosion";
    }

    /// <summary>
    /// پرتاب‌کننده نارنجک بازیکن: شمارش محدود، کول‌داون، Projectile فیزیکی Pool‌شده.
    /// </summary>
    public sealed class GrenadeThrower : MonoBehaviour
    {
        [Header("داده")]
        [SerializeField] private GrenadeDataSO data;

        private int _count;
        private int _maxCount = 4;
        private float _cooldownTimer;

        /// <summary>تعداد فعلی.</summary>
        public int Count => _count;
        /// <summary>(تعداد فعلی)</summary>
        public event Action<int> OnCountChanged;

        /// <summary>مقداردهی با داده و شروع اولیه.</summary>
        public void Initialize(GrenadeDataSO grenadeData, int startCount, int maxCount)
        {
            data = grenadeData;
            _maxCount = maxCount;
            _count = Mathf.Clamp(startCount, 0, _maxCount);
            OnCountChanged?.Invoke(_count);
        }

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        /// <summary>پرتاب در جهت داده‌شده (معمولاً دوربین). false اگر نداریم/کول‌داون.</summary>
        public bool TryThrow(Vector3 origin, Vector3 direction)
        {
            if (data == null || _count <= 0 || _cooldownTimer > 0f) return false;
            if (!ServiceLocator.TryGet(out PoolManager pool)) return false;

            GameObject prefab = data.projectilePrefab;
            if (prefab == null)
            {
                GameLog.Warn("[Grenade] projectilePrefab ندارد؛ نارنجک پرتاب نشد.");
                return false;
            }

            GameObject go = pool.Spawn(prefab, origin + direction * 0.4f + Vector3.up * 0.1f, Quaternion.identity);
            if (go == null) return false;

            if (go.TryGetComponent(out GrenadeProjectile proj))
            {
                proj.Launch(direction * data.throwSpeed + Vector3.up * data.upForce, data);
            }

            _count--;
            _cooldownTimer = data.throwCooldown;
            OnCountChanged?.Invoke(_count);
            Audio.AudioManager.PlaySfxShielded("sfx.grenade.throw", origin);
            return true;
        }

        /// <summary>افزودن نارنجک با سقف (Pickup).</summary>
        public void Add(int amount)
        {
            _count = Mathf.Min(_maxCount, _count + Mathf.Max(0, amount));
            OnCountChanged?.Invoke(_count);
        }
    }

    /// <summary>
    /// پرتابه نارنجک: فیوز، انفجار ناحیه‌ای (با Falloff)، هشدار بصری کوتاه، صدا و لرزش نزدیک.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GrenadeProjectile : MonoBehaviour, IPoolable
    {
        private Rigidbody _rb;
        private GrenadeDataSO _data;
        private float _fuseTimer;
        private bool _launched;
        private PoolableObject _poolable;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _poolable = GetComponent<PoolableObject>();
        }

        /// <summary>پرتاب با سرعت اولیه و داده.</summary>
        public void Launch(Vector3 velocity, GrenadeDataSO data)
        {
            _data = data;
            _fuseTimer = data.fuseSeconds;
            _launched = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.AddForce(velocity, ForceMode.VelocityChange);
        }

        public void OnSpawnedFromPool() { }
        public void OnReturnedToPool() { _launched = false; _rb.linearVelocity = Vector3.zero; }

        private void Update()
        {
            if (!_launched) return;
            _fuseTimer -= Time.deltaTime;
            if (_fuseTimer <= 0f) Explode();
        }

        private void Explode()
        {
            _launched = false;

            ExplosionUtility.DealAreaDamage(
                transform.position, _data.radius, _data.damage,
                DamageType.Explosion, this, GameplayLayers.ExplosionMask);

            if (ServiceLocator.TryGet(out PoolManager pool) && _data.explosionFxPrefab != null)
                pool.Spawn(_data.explosionFxPrefab, transform.position, Quaternion.identity);

            Audio.AudioManager.PlaySfxShielded(_data.explosionSfxKey, transform.position);
            AISoundBus.Report(transform.position, 45f, 1f); // صدای بسیار بلند برای AI

            // لرزش دوربین و هپتیک در نزدیکی انفجار
            Player.Controllers.CameraShaker.AddProximityImpulse(transform.position, 30f);
            Utilities.HapticsUtility.Short();

            if (_poolable != null) _poolable.Release();
            else gameObject.SetActive(false);
        }
    }
}
