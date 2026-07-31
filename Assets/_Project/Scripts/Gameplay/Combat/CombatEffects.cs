using FogWalker.Optimization;
using UnityEngine;

namespace FogWalker.Gameplay.Combat
{
    /// <summary>نوع سطح برای انتخاب افکت برخورد.</summary>
    public enum SurfaceType { Concrete, Metal, Wood, Glass, Dirt }

    /// <summary>برچسب نوع سطح روی آبجکت‌های محیط؛ در نبود آن، بتن فرض می‌شود.</summary>
    public sealed class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surface = SurfaceType.Concrete;
        public SurfaceType Surface => surface;
    }

    /// <summary>
    /// کتابخانه افکت برخورد بر اساس سطح؛ پری‌فب‌ها باید PooledFX داشته باشند.
    /// </summary>
    [CreateAssetMenu(fileName = "ImpactLibrary", menuName = "FogWalker/Combat/Impact Library")]
    public sealed class ImpactLibrarySO : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry { public SurfaceType surface; public GameObject prefab; }

        [Tooltip("پری‌فب پیش‌فرض وقتی سطح خاصی تعریف نشده")]
        public GameObject defaultPrefab;
        public Entry[] entries = System.Array.Empty<Entry>();

        /// <summary>پری‌فب افکت برای نوع سطح؛ null اگر چیزی تعریف نشده.</summary>
        public GameObject Get(SurfaceType surface)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].surface == surface && entries[i].prefab != null)
                    return entries[i].prefab;
            return defaultPrefab;
        }
    }

    /// <summary>
    /// افکت کوتاه‌عمر Pool‌شده (برخورد گلوله، فلاش دهانه سلاح): Scale/Fade ساده و بازگشت خودکار به Pool.
    /// جایگزین کم‌هزینه Particle برای تعداد زیاد؛ در آینده می‌توان Particle واقعی جایگذاشت بدون شکستن سیستم.
    /// </summary>
    public sealed class PooledFX : MonoBehaviour, IPoolable
    {
        [SerializeField, Tooltip("مدت نمایش (ثانیه)")] private float lifetime = 0.08f;
        [SerializeField] private float startScale = 0.3f;
        [SerializeField] private float endScale = 0.9f;
        [SerializeField, Tooltip("بیلبورد به سمت دوربین")] private bool billboard = true;

        private float _timer;
        private SpriteRenderer _sprite;
        private Color _baseColor = Color.white;
        private PoolableObject _poolable;

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite != null) _baseColor = _sprite.color;
            _poolable = GetComponent<PoolableObject>();
        }

        public void OnSpawnedFromPool()
        {
            _timer = lifetime;
            transform.localScale = Vector3.one * startScale;
            if (_sprite != null) _sprite.color = _baseColor;
        }

        public void OnReturnedToPool() { }

        private void Update()
        {
            _timer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_timer / lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

            if (_sprite != null)
            {
                Color c = _baseColor;
                c.a = Mathf.Lerp(_baseColor.a, 0f, t);
                _sprite.color = c;
            }

            if (billboard && Camera.main != null)
                transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

            if (_timer <= 0f)
            {
                if (_poolable != null) _poolable.Release();
                else gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// رد لایه گلوله (LineRenderer) Pool‌شده؛ از دهانه سلاح تا نقطه برخورد و ناپدید شدن سریع.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PooledTracer : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 0.05f;

        private LineRenderer _line;
        private float _timer;
        private PoolableObject _poolable;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _poolable = GetComponent<PoolableObject>();
        }

        /// <summary>تنظیم مسیر ردیاب بلافاصله پس از Spawn.</summary>
        public void SetLine(Vector3 from, Vector3 to)
        {
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);
        }

        public void OnSpawnedFromPool() => _timer = lifetime;
        public void OnReturnedToPool() { }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                if (_poolable != null) _poolable.Release();
                else gameObject.SetActive(false);
            }
        }
    }
}
