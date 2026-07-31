using System;
using System.Collections;
using System.Collections.Generic;
using FogWalker.Core;
using FogWalker.Gameplay.AI;
using FogWalker.Gameplay.Player;
using FogWalker.Optimization;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Missions
{
    /// <summary>مدل فعال‌سازی یک ناحیه Spawn.</summary>
    public enum SpawnTrigger { PlayerEnter, OnObjectiveStart, Manual }

    /// <summary>تعریف یک موج پیش‌فرض داخل ناحیه.</summary>
    [Serializable]
    public sealed class WaveEntry
    {
        public EnemyArchetypeDataSO archetype;
        public int count = 3;
        public float delayBetween = 0.6f;
        [Tooltip("تأخیر اولیه موج پس از فعال‌سازی ناحیه")] public float initialDelay;
    }

    /// <summary>
    /// ناحیه Spawn: تریگر + پیکربندی موج + سقف هم‌زمان + بودجه جهانی.
    /// دشمنان از PoolManager آمده و با مرگ به آن برمی‌گردند؛ بدون Instantiate در نبرد.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpawnZone : MonoBehaviour
    {
        [Header("هویت")]
        [SerializeField, Tooltip("شناسه گروه برای اهداف EliminateGroup")]
        private string groupId = "group1";

        [Header("تریگر")]
        [SerializeField] private SpawnTrigger trigger = SpawnTrigger.PlayerEnter;
        [SerializeField, Tooltip("برای OnObjectiveStart: id هدف")]
        private string objectiveId;

        [Header("موج")]
        [SerializeField] private WaveEntry[] entries = Array.Empty<WaveEntry>();

        [Header("نقاط Spawn (اگر خالی باشد، مرکز ناحیه)")]
        [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();

        [Header("محدودیت")]
        [SerializeField] private int maxAliveForZone = 6;
        [SerializeField] private float spawnRadiusWhenNoPoints = 4f;

        private bool _activated;
        private int _alive;
        private SpawnManager _owner;

        /// <summary>شناسه گروه.</summary>
        public string GroupId => groupId;
        /// <summary>تعداد زنده‌های این ناحیه.</summary>
        public int AliveCount => _alive;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        /// <summary>ثبت در مدیر Spawn (توسط SpawnManager.Collect صدا زده می‌شود).</summary>
        public void Bind(SpawnManager owner)
        {
            _owner = owner;
        }

        /// <summary>فعال‌سازی دستی/هدفی.</summary>
        public void Activate()
        {
            if (_activated) return;
            _activated = true;
            if (trigger == SpawnTrigger.PlayerEnter) return; // در TriggerEnter جدا فعال می‌شود
            StartCoroutine(SpawnAllWaves());
        }

        /// <summary>فعال‌سازی رویداد Objective (از MissionManager).</summary>
        public void NotifyObjectiveStarted(string startedId)
        {
            if (trigger != SpawnTrigger.OnObjectiveStart || objectiveId != startedId) return;
            Activate();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_activated || trigger != SpawnTrigger.PlayerEnter) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            _activated = true;
            StartCoroutine(SpawnAllWaves());
        }

        private IEnumerator SpawnAllWaves()
        {
            for (int w = 0; w < entries.Length; w++)
            {
                WaveEntry entry = entries[w];
                if (entry == null || entry.archetype == null) continue;

                if (entry.initialDelay > 0f)
                    yield return new WaitForSeconds(entry.initialDelay);

                for (int i = 0; i < entry.count; i++)
                {
                    // بودجه: اگر ناحیه/جهان پر است، کمی صبر و تلاش مجدد (به جای Spawn اضافی)
                    int guard = 0;
                    while ((_alive >= maxAliveForZone || !SpawnManager.HasGlobalBudget) && guard++ < 60)
                        yield return new WaitForSeconds(0.5f);

                    SpawnOne(entry.archetype);
                    yield return new WaitForSeconds(entry.delayBetween);
                }
            }
        }

        private void SpawnOne(EnemyArchetypeDataSO archetype)
        {
            if (!ServiceLocator.TryGet(out PoolManager pool)) return;
            if (_owner == null || _owner.EnemyPrefab == null) { GameLog.Error("[Spawn] EnemyPrefab پایه تنظیم نشده!"); return; }

            Vector3 pos = PickSpawnPosition();
            GameObject go = pool.Spawn(_owner.EnemyPrefab, pos, Quaternion.identity);
            if (go == null) return;

            var brain = go.GetComponent<EnemyBrain>();
            if (brain == null)
            {
                GameLog.Error("[Spawn] پری‌فب دشمن EnemyBrain ندارد!");
                pool.Despawn(go);
                return;
            }

            brain.SpawnGroupId = groupId;
            brain.Configure(archetype, _owner.PlayerTransform);
            var motor = go.GetComponent<EnemyMotor>();
            if (motor != null) motor.ResetTo(pos);

            _alive++;
            _owner.RegisterEnemySpawned(brain);

            // غنای بصری: ویژوال آرکی‌تایپ اگر موجود باشد
            var visualizer = go.GetComponent<EnemyVisualizer>();
            if (visualizer != null) visualizer.Apply(archetype);
        }

        private Vector3 PickSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;

            Vector2 rnd = UnityEngine.Random.insideUnitCircle * spawnRadiusWhenNoPoints;
            return transform.position + new Vector3(rnd.x, 0f, rnd.y);
        }

        /// <summary>گزارش مرگ یکی از اعضا (توسط SpawnManager).</summary>
        public void NotifyMemberDied()
        {
            _alive = Mathf.Max(0, _alive - 1);
        }
    }

    /// <summary>
    /// مدیر Spawn مرحله: جمع‌آوری ناحیه‌ها، بودجه جهانی دشمنان زنده (با سقف سختی/کیفیت)،
    /// اتصال SpawnZoneها به Objectiveها، و گزارش تلفات برای بودجه.
    /// </summary>
    public sealed class SpawnManager : MonoBehaviour
    {
        [Header("سیم‌کشی")]
        [SerializeField, Tooltip("پری‌فب پایه دشمن (Container با Brain/Motor/Perception/Combat/Health)")]
        private GameObject enemyPrefab;

        [Header("بودجه جهانی")]
        [SerializeField, Tooltip("سقف دشمنان زنده هم‌زمان (بودجه عملکرد موبایل)")]
        private int globalAliveCap = 8;

        private readonly HashSet<EnemyBrain> _aliveEnemies = new HashSet<EnemyBrain>(32);
        private SpawnZone[] _zones;

        /// <summary>ترنسفرم بازیکن (توسط Bootstrapper تزریق می‌شود).</summary>
        public Transform PlayerTransform { get; set; }
        /// <summary>پری‌فب پایه دشمن.</summary>
        public GameObject EnemyPrefab => enemyPrefab;
        /// <summary>بودجه جهانی آزاد؟</summary>
        public static bool HasGlobalBudget => _globalAlive < _globalCap;
        private static int _globalAlive;
        private static int _globalCap = 8;

        private void Awake()
        {
            _globalCap = globalAliveCap;
            _zones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
            foreach (var z in _zones)
                z.Bind(this);
        }

        private void OnEnable()
        {
            EnemyLifecycleEvents.OnEnemyDied += HandleEnemyDied;
            if (ServiceLocator.TryGet(out MissionManager mission))
                mission.OnObjectiveStartedId += HandleObjectiveStarted;
        }

        private void OnDisable()
        {
            EnemyLifecycleEvents.OnEnemyDied -= HandleEnemyDied;
            if (ServiceLocator.TryGet(out MissionManager mission))
                mission.OnObjectiveStartedId -= HandleObjectiveStarted;
            _globalAlive = 0; // خروج مرحله = پاک شدن بودجه
        }

        private void Start()
        {
            // subscribe مجدد بعد از OnEnable (ترتیب سرویس‌ها)
            if (ServiceLocator.TryGet(out MissionManager mission))
            {
                mission.OnObjectiveStartedId -= HandleObjectiveStarted;
                mission.OnObjectiveStartedId += HandleObjectiveStarted;
            }
        }

        private void HandleObjectiveStarted(string objectiveId)
        {
            foreach (var z in _zones)
                if (z != null) z.NotifyObjectiveStarted(objectiveId);
        }

        /// <summary>فعال‌سازی دستی یک ناحیه (مثلاً از Cutscene).</summary>
        public void ActivateZone(string groupId)
        {
            foreach (var z in _zones)
                if (z != null && z.GroupId == groupId) z.Activate();
        }

        /// <summary>حذف همه دشمنان زنده (پایان مرحله/ری‌لود — بدون Destroy).</summary>
        public void DespawnAll()
        {
            if (!ServiceLocator.TryGet(out PoolManager pool)) return;
            foreach (var enemy in _aliveEnemies)
                if (enemy != null) pool.Despawn(enemy.gameObject);
            _aliveEnemies.Clear();
            _globalAlive = 0;
        }

        private void HandleEnemyDied(EnemyBrain enemy)
        {
            if (enemy == null) return;
            _aliveEnemies.Remove(enemy);
            _globalAlive = Mathf.Max(0, _globalAlive - 1);

            foreach (var z in _zones)
                if (z != null && z.GroupId == enemy.SpawnGroupId)
                    z.NotifyMemberDied();
        }

        /// <summary>ثبت Spawn (افزایش بودجه).</summary>
        public void RegisterEnemySpawned(EnemyBrain enemy)
        {
            _aliveEnemies.Add(enemy);
            _globalAlive++;
        }
    }

    /// <summary>
    /// نمایشگر بصری Placeholder دشمن بر اساس آرکی‌تایپ (رنگ/اندازه) — قابل جایگزینی مدل نهایی.
    /// </summary>
    public sealed class EnemyVisualizer : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;

        /// <summary>اعمال ظاهر آرکی‌تایپ.</summary>
        public void Apply(EnemyArchetypeDataSO archetype)
        {
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<Renderer>();
            if (bodyRenderer == null || archetype == null) return;

            Color c;
            switch (archetype.archetype)
            {
                case EnemyArchetype.Rusher: c = new Color(0.85f, 0.55f, 0.2f); break;
                case EnemyArchetype.Heavy: c = new Color(0.55f, 0.2f, 0.2f); transform.localScale = new Vector3(1.25f, 1.15f, 1.25f); break;
                default: c = new Color(0.45f, 0.5f, 0.55f); break;
            }
            bodyRenderer.material.color = c;
        }
    }
}
