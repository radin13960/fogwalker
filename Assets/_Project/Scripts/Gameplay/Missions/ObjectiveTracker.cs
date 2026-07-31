using System;

namespace FogWalker.Gameplay.Missions
{
    /// <summary>
    /// ردیاب اهداف (POCO — قابل تست واحد): ترتیبی پیش می‌رود؛
    /// Collect شمارش دارد؛ Defend تایمر دارد؛ بازیابی از چک‌پوینت با RestoreIndex.
    /// </summary>
    public sealed class ObjectiveTracker
    {
        private readonly ObjectiveDef[] _defs;

        /// <summary>اندیس هدف فعال فعلی؛ -1 یعنی همه کامل شده.</summary>
        public int CurrentIndex { get; private set; }
        /// <summary>هدف فعال فعلی؛ null اگر تمام شده.</summary>
        public ObjectiveDef Current => CurrentIndex >= 0 && CurrentIndex < _defs.Length ? _defs[CurrentIndex] : null;
        /// <summary>پیشرفت هدف فعلی (Collect: تعداد؛ Defend: ثانیه سپری‌شده).</summary>
        public float CurrentProgress { get; private set; }
        /// <summary>همه اهداف کامل شد؟</summary>
        public bool IsComplete => CurrentIndex < 0 || CurrentIndex >= _defs.Length;

        /// <summary>(هدف جدید فعال، اندیس)</summary>
        public event Action<ObjectiveDef, int> OnObjectiveStarted;
        /// <summary>(هدف کامل‌شده، اندیس)</summary>
        public event Action<ObjectiveDef, int> OnObjectiveCompleted;
        /// <summary>(هدف فعال، پیشرفت فعلی)</summary>
        public event Action<ObjectiveDef, float> OnObjectiveProgress;

        /// <summary>ساخت ردیاب از تعریف‌ها؛ اولین هدف فوراً فعال می‌شود.</summary>
        public ObjectiveTracker(ObjectiveDef[] defs)
        {
            _defs = defs ?? Array.Empty<ObjectiveDef>();
            CurrentIndex = 0;
            CurrentProgress = 0f;
            if (_defs.Length == 0) CurrentIndex = -1;
        }

        /// <summary>شروع اعلامی رویدادها (بعد از subscribe صدا زده شود).</summary>
        public void RaiseCurrentStarted()
        {
            if (Current != null) OnObjectiveStarted?.Invoke(Current, CurrentIndex);
        }

        /// <summary>ثانیه‌به‌ثانیه برای Defend.</summary>
        public void Tick(float deltaTime)
        {
            ObjectiveDef cur = Current;
            if (cur == null || cur.type != ObjectiveType.Defend) return;

            CurrentProgress += deltaTime;
            OnObjectiveProgress?.Invoke(cur, CurrentProgress);
            if (CurrentProgress >= cur.timeSeconds)
                CompleteCurrent();
        }

        /// <summary>رویداد Reach با id.</summary>
        public bool NotifyReach(string id)
        {
            ObjectiveDef cur = Current;
            if (cur == null || cur.type != ObjectiveType.Reach || !IdsMatch(cur, id)) return false;
            CompleteCurrent();
            return true;
        }

        /// <summary>رویداد Interact با id.</summary>
        public bool NotifyInteract(string id)
        {
            ObjectiveDef cur = Current;
            if (cur == null || cur.type != ObjectiveType.Interact || !IdsMatch(cur, id)) return false;
            CompleteCurrent();
            return true;
        }

        /// <summary>رویداد Pickup آیتم مأموریت با id (Collect با شمارنده).</summary>
        public bool NotifyPickup(string id)
        {
            ObjectiveDef cur = Current;
            if (cur == null || cur.type != ObjectiveType.Collect || !IdsMatch(cur, id)) return false;

            CurrentProgress += 1f;
            OnObjectiveProgress?.Invoke(cur, CurrentProgress);
            if ((int)CurrentProgress >= Mathf_Max(1, cur.requiredCount))
                CompleteCurrent();
            return true;
        }

        /// <summary>رویداد مرگ دشمن از گروه Spawn هدف.</summary>
        public bool NotifyKill(string groupId)
        {
            ObjectiveDef cur = Current;
            if (cur == null || cur.type != ObjectiveType.EliminateGroup) return false;
            string target = !string.IsNullOrEmpty(cur.targetGroupId) ? cur.targetGroupId : cur.id;
            if (string.IsNullOrEmpty(groupId) || groupId != target) return false;

            CurrentProgress += 1f;
            OnObjectiveProgress?.Invoke(cur, CurrentProgress);

            // کامل شدن وقتی همه اعضای گروه مردند — با requiredCount تعیین می‌شود
            if ((int)CurrentProgress >= Mathf_Max(1, cur.requiredCount))
                CompleteCurrent();
            return true;
        }

        /// <summary>بازیابی از چک‌پوینت: همه اهداف قبل از این اندیس کامل علامت می‌خورند.</summary>
        public void RestoreBeforeIndex(int index)
        {
            if (index <= 0) return;
            CurrentIndex = Math.Min(index, _defs.Length);
            CurrentProgress = 0f;
            if (CurrentIndex >= _defs.Length) CurrentIndex = _defs.Length; // یعنی پایان
        }

        private void CompleteCurrent()
        {
            ObjectiveDef done = Current;
            int doneIndex = CurrentIndex;
            if (done == null) return;

            OnObjectiveCompleted?.Invoke(done, doneIndex);
            CurrentIndex++;
            CurrentProgress = 0f;
            if (Current != null)
                OnObjectiveStarted?.Invoke(Current, CurrentIndex);
        }

        private static bool IdsMatch(ObjectiveDef def, string incoming)
        {
            return def != null && !string.IsNullOrEmpty(incoming) && def.id == incoming;
        }

        private static int Mathf_Max(int a, int b) => a > b ? a : b;
    }
}
