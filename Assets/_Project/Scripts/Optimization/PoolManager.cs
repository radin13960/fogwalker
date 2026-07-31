using System.Collections.Generic;
using FogWalker.Core;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Optimization
{
    /// <summary>کال‌بک‌های چرخه عمر آبجکت Pool.</summary>
    public interface IPoolable
    {
        void OnSpawnedFromPool();
        void OnReturnedToPool();
    }

    /// <summary>اتصال نمونه به Pool و پری‌فب منشأ (برای Despawn خودکار).</summary>
    public sealed class PoolableObject : MonoBehaviour
    {
        /// <summary>پری‌فب منشأ (هنگام Spawn تنظیم می‌شود).</summary>
        public GameObject SourcePrefab { get; set; }

        /// <summary>برگرداندن این آبجکت به Pool.</summary>
        public void Release()
        {
            if (ServiceLocator.TryGet(out PoolManager pool)) pool.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Pool عمومی برای گلوله‌ها، پوکه‌ها، افکت‌ها، دشمنان و Popupها — جلوگیری از Instantiate/Destroy در نبرد.
    /// ظرفیت اولیه/حداکثر و رفتار هنگام پرشدن (بازیافت قدیمی‌ترین) مشخص است.
    /// </summary>
    public sealed class PoolManager : MonoBehaviour
    {
        [Header("محدودیت‌ها")]
        [SerializeField, Tooltip("حداکثر کل آبجکت‌های فعال هم‌زمان همه Poolها (بودجه موبایل)")]
        private int globalActiveCap = 256;

        private sealed class Pool
        {
            public GameObject Prefab;
            public readonly Queue<GameObject> Idle = new Queue<GameObject>(16);
            public int ActiveCount;
        }

        private readonly Dictionary<GameObject, Pool> _byPrefab = new Dictionary<GameObject, Pool>(32);
        private readonly Dictionary<GameObject, Pool> _byInstance = new Dictionary<GameObject, Pool>(128);
        private Transform _root;
        private int _globalActive;

        private void Awake()
        {
            _root = new GameObject("Pools").transform;
            _root.SetParent(transform, false);
        }

        /// <summary>پیش‌گرم‌کردن Pool برای جلوگیری از هیچ الحاقی اول نبرد.</summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            Pool pool = GetOrCreatePool(prefab);
            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateInstance(pool);
                pool.Idle.Enqueue(instance);
            }
        }

        /// <summary>گرفتن نمونه از Pool (یا ساخت جدید در صورت خلأ). موقعیت/چرخش تنظیم می‌شود.</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) { GameLog.Error("[Pool] پری‌فب null است!"); return null; }

            Pool pool = GetOrCreatePool(prefab);
            if (_globalActive >= globalActiveCap)
            {
                GameLog.Warn("[Pool] سقف فعال پر است؛ درخواست Spawn رد شد.");
                return null;
            }

            GameObject instance = pool.Idle.Count > 0 ? pool.Idle.Dequeue() : CreateInstance(pool);
            if (instance == null) return null;

            Transform t = instance.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(position, rotation);

            var poolable = instance.GetComponent<PoolableObject>();
            if (poolable == null) poolable = instance.AddComponent<PoolableObject>();
            poolable.SourcePrefab = prefab;

            instance.SetActive(true);
            pool.ActiveCount++;
            _globalActive++;

            // کال‌بک روی همه IPoolableهای درخت
            foreach (var p in instance.GetComponentsInChildren<IPoolable>(true))
                p.OnSpawnedFromPool();

            return instance;
        }

        /// <summary>برگرداندن نمونه به Pool (اگر Pool‌شده نبود، غیرفعالش می‌کند).</summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            foreach (var p in instance.GetComponentsInChildren<IPoolable>(true))
                p.OnReturnedToPool();

            if (_byInstance.TryGetValue(instance, out Pool pool))
            {
                instance.SetActive(false);
                instance.transform.SetParent(_root, false);
                pool.Idle.Enqueue(instance);
                pool.ActiveCount = Mathf.Max(0, pool.ActiveCount - 1);
                _globalActive = Mathf.Max(0, _globalActive - 1);
                return; // نگاشت نمونه→Pool ماندگار است (نمونه‌ها هرگز Destroy نمی‌شوند)
            }

            instance.SetActive(false);
        }

        private Pool GetOrCreatePool(GameObject prefab)
        {
            if (!_byPrefab.TryGetValue(prefab, out Pool pool))
            {
                pool = new Pool { Prefab = prefab };
                _byPrefab.Add(prefab, pool);
            }
            return pool;
        }

        private GameObject CreateInstance(Pool pool)
        {
            GameObject instance = Instantiate(pool.Prefab, _root);
            instance.name = pool.Prefab.name + "_Pooled";
            instance.SetActive(false);
            _byInstance.Add(instance, pool);
            return instance;
        }
    }
}
