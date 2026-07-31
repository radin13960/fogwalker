using System;
using FogWalker.Core;
using FogWalker.Gameplay.AI;
using FogWalker.Save;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Missions
{
    /// <summary>آمار پایان مرحله برای صفحه نتایج.</summary>
    public struct MissionStats
    {
        public float TimeSeconds;
        public float Accuracy;
        public int Kills;
        public int ObjectivesTotal;
        public int ObjectivesDone;
        public string UnlockedNextLevelId; // null/empty اگر چیزی باز نشد
    }

    /// <summary>
    /// مدیر مأموریت مرحله: اجرای ObjectiveTracker، شمارش آمار، موج‌ها (از طریق SpawnZoneها)،
    /// تکمیل مرحله (بازکردن مرحله بعد در Save + رویداد نمایش)، چک‌پوینت‌نویسی و موسیقی.
    /// </summary>
    public sealed class MissionManager : MonoBehaviour
    {
        [Header("داده")]
        [SerializeField] private MissionDataSO mission;
        [SerializeField] private SceneCatalog sceneCatalog;

        private ObjectiveTracker _tracker;
        private float _missionTimer;
        private int _kills;
        private bool _finished;

        /// <summary>داده مأموریت.</summary>
        public MissionDataSO Mission => mission;
        /// <summary>ردیاب اهداف.</summary>
        public ObjectiveTracker Tracker => _tracker;
        /// <summary>آیا مأموریت تمام شده؟</summary>
        public bool IsFinished => _finished;

        /// <summary>هدف فعلی تغییر کرد (برای HUD): (کلید متن، ایندکس/تعداد برای "...").</summary>
        public event Action<ObjectiveDef, float> OnObjectiveUpdated;
        /// <summary>مأموریت کامل شد (برای UI و Save).</summary>
        public event Action<MissionStats> OnMissionCompleted;
        /// <summary>هدف جدیدی شروع شد (برای تریگر SpawnZone های موج).</summary>
        public event Action<string> OnObjectiveStartedId;

        private void Awake()
        {
            if (mission == null)
            {
                GameLog.Error("[Mission] MissionDataSO اختصاص داده نشده است!");
                enabled = false;
                return;
            }

            _tracker = new ObjectiveTracker(mission.objectives);
            _tracker.OnObjectiveStarted += HandleObjectiveStarted;
            _tracker.OnObjectiveCompleted += HandleObjectiveCompleted;
            _tracker.OnObjectiveProgress += HandleObjectiveProgress;
        }

        private void OnEnable()
        {
            EnemyLifecycleEvents.OnEnemyDied += HandleEnemyDied;
            InteractionEvents.OnInteractedId += NotifyInteract;
        }

        private void OnDisable()
        {
            EnemyLifecycleEvents.OnEnemyDied -= HandleEnemyDied;
            InteractionEvents.OnInteractedId -= NotifyInteract;
        }

        private void Start()
        {
            _tracker.RaiseCurrentStarted();
            StartCoroutine(LateWireRoutine());
        }

        private System.Collections.IEnumerator LateWireRoutine()
        {
            // یک فریم صبر تا SpawnZoneها و HUD subscribe کنند
            yield return null;
            if (_tracker.Current != null)
                OnObjectiveStartedId?.Invoke(_tracker.Current.id);
        }

        private void Update()
        {
            if (_finished) return;
            _missionTimer += Time.deltaTime;
            _tracker.Tick(Time.deltaTime);

            if (_tracker.IsComplete && !_finished)
                Finish();
        }

        // ---------- API رویدادها (از Triggerها/Interactableها) ----------

        /// <summary>رویداد رسیدن به نقطه.</summary>
        public void NotifyReach(string id) { if (!_finished && _tracker.NotifyReach(id)) GameLog.Info($"[Mission] Reach: {id}"); }
        /// <summary>رویداد تعامل.</summary>
        public void NotifyInteract(string id) { if (!_finished && _tracker.NotifyInteract(id)) GameLog.Info($"[Mission] Interact: {id}"); }
        /// <summary>رویداد جمع‌آوری آیتم.</summary>
        public void NotifyPickup(string id) { if (!_finished && _tracker.NotifyPickup(id)) GameLog.Info($"[Mission] Pickup: {id}"); }

        /// <summary>تلاش برای بازیابی هدف ادم (نه استفاده عادی) — برای دیباگ.</summary>
        public void DebugCompleteCurrent() { if (!_finished) _tracker.NotifyReach(_tracker.Current?.id ?? ""); }

        // ---------- مسیرها ----------

        private void HandleObjectiveStarted(ObjectiveDef def, int index)
        {
            OnObjectiveStartedId?.Invoke(def.id);
            OnObjectiveUpdated?.Invoke(def, _tracker.CurrentProgress);
        }

        private void HandleObjectiveCompleted(ObjectiveDef def, int index)
        {
            OnObjectiveUpdated?.Invoke(def, -1f); // -1 = کامل
            Audio.AudioManager.PlaySfxShielded("sfx.objective.done", transform.position);
        }

        private void HandleObjectiveProgress(ObjectiveDef def, float progress)
        {
            OnObjectiveUpdated?.Invoke(def, progress);
        }

        private void HandleEnemyDied(EnemyBrain enemy)
        {
            _kills++;
            if (!string.IsNullOrEmpty(enemy.SpawnGroupId))
                _tracker.NotifyKill(enemy.SpawnGroupId);
        }

        private void Finish()
        {
            _finished = true;

            // آمار دقت از کنترلر مبارزه بازیکن
            float accuracy = 0f;
            var player = FindFirstObjectByType<Player.PlayerCombatController>();
            if (player != null) accuracy = player.Accuracy;

            var stats = new MissionStats
            {
                TimeSeconds = _missionTimer,
                Accuracy = accuracy,
                Kills = _kills,
                ObjectivesTotal = mission.objectives.Length,
                ObjectivesDone = mission.objectives.Length,
            };

            // بازکردن مرحله بعد در Save
            string unlockedNext = null;
            var save = ServiceLocator.TryGet<ISaveSystem>();
            if (save != null && sceneCatalog != null)
            {
                unlockedNext = ProgressUnlocker.UnlockNextLevel(save.Data.progress, sceneCatalog, mission.levelId);
                ProgressUnlocker.RecordCompletion(save.Data.stats, mission.levelId, stats);
                ProgressUnlocker.ClearCheckpoint(save.Data.progress, mission.levelId);
                save.Save();
            }

            stats.UnlockedNextLevelId = unlockedNext;
            Audio.AudioManager.SetMoodShielded(MusicMood.Victory);
            OnMissionCompleted?.Invoke(stats);
        }
    }

    /// <summary>تغییرات پیشرفت (خالص، قابل تست) — جداشده از MissionManager برای تست واحد.</summary>
    public static class ProgressUnlocker
    {
        /// <summary>
        /// مرحله بعدی را در Save باز می‌کند و شناسه‌اش را برمی‌گرداند (null = آخرین مرحله بود).
        /// </summary>
        public static string UnlockNextLevel(ProgressData progress, SceneCatalog catalog, string completedLevelId)
        {
            if (progress == null || catalog?.levels == null) return null;
            for (int i = 0; i < catalog.levels.Length; i++)
            {
                if (catalog.levels[i].levelId != completedLevelId) continue;
                if (i + 1 < catalog.levels.Length)
                {
                    string nextId = catalog.levels[i + 1].levelId;
                    if (!progress.unlockedLevelIds.Contains(nextId))
                        progress.unlockedLevelIds.Add(nextId);
                    return nextId;
                }
                return null;
            }
            return null;
        }

        /// <summary>ثبت آمار مرحله (بهترین‌ها) در StatsData.</summary>
        public static void RecordCompletion(StatsData stats, string levelId, MissionStats s)
        {
            if (stats == null) return;
            LevelStatRecord rec = stats.levelRecords.Find(r => r.levelId == levelId);
            if (rec == null)
            {
                rec = new LevelStatRecord { levelId = levelId, bestTimeSeconds = s.TimeSeconds, bestAccuracy = s.Accuracy, bestKills = s.Kills };
                stats.levelRecords.Add(rec);
            }
            else
            {
                if (s.TimeSeconds < rec.bestTimeSeconds || rec.bestTimeSeconds <= 0f) rec.bestTimeSeconds = s.TimeSeconds;
                if (s.Accuracy > rec.bestAccuracy) rec.bestAccuracy = s.Accuracy;
                if (s.Kills > rec.bestKills) rec.bestKills = s.Kills;
            }
            rec.timesCompleted++;
            stats.totalKills += s.Kills;
        }

        /// <summary>پاک‌کردن چک‌پوینت پس از تکمیل موفق مرحله.</summary>
        public static void ClearCheckpoint(ProgressData progress, string levelId)
        {
            if (progress == null) return;
            if (progress.checkpointLevelId == levelId)
            {
                progress.checkpointLevelId = string.Empty;
                progress.lastCheckpointId = string.Empty;
                progress.lastObjectiveIndex = 0;
            }
        }
    }

    /// <summary>گذرگاه رویدادهای تعامل با id (برای اتصال Interactable به ObjectiveSystem).</summary>
    public static class InteractionEvents
    {
        /// <summary>(شناسه id تعامل‌شده)</summary>
        public static event Action<string> OnInteractedId;
        public static void Raise(string id) => OnInteractedId?.Invoke(id);
    }

    /// <summary>مودهای موسیقی پویا.</summary>
    public enum MusicMood { Exploration, Tension, Combat, Victory }
}
