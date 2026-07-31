using System;
using FogWalker.Core;
using FogWalker.Gameplay.Player;
using FogWalker.Save;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Missions
{
    /// <summary>
    /// مدیر چک‌پوینت: ثبت آخرین چک‌پوینت + اندیس هدف در SaveData،
    /// و اعمال Respawn (Teleport بازیکن، بازیابی اهداف، ریست مهمات) در شروع صحنه و پس از مرگ.
    /// </summary>
    public sealed class CheckpointManager : MonoBehaviour
    {
        [Header("داده")]
        [SerializeField] private MissionDataSO mission;

        private PlayerController _player;

        /// <summary>رویداد ثبت چک‌پوینت (برای HUD Toast).</summary>
        public event Action<string> OnCheckpointCaptured;

        /// <summary>ثبت چک‌پوینت با آخرین هدفِ فعال فعلی.</summary>
        public void Capture(string checkpointId)
        {
            if (!ServiceLocator.TryGet(out ISaveSystem save)) return;
            if (!ServiceLocator.TryGet(out MissionManager missionManager)) return;

            int objectiveIndex = missionManager.Tracker != null ? missionManager.Tracker.CurrentIndex : 0;

            save.Data.progress.lastCheckpointId = checkpointId;
            save.Data.progress.checkpointLevelId = mission != null ? mission.levelId : string.Empty;
            save.Data.progress.lastObjectiveIndex = objectiveIndex;
            save.Data.progress.hasSave = true;
            save.Save();

            OnCheckpointCaptured?.Invoke(checkpointId);
            GameLog.Info($"[Checkpoint] ذخیره شد: {checkpointId} در اندیس هدف {objectiveIndex}");
        }

        /// <summary>
        /// اعمال وضعیت چک‌پوینت ذخیره‌شده (اگر متعلق به همین مرحله است):
        /// Teleport بازیکن، جلو بردن ObjectiveTracker، ریست مهمات.
        /// </summary>
        public bool ApplySavedCheckpointIfAny(PlayerController player, MissionManager missionManager)
        {
            _player = player;
            if (!ServiceLocator.TryGet(out ISaveSystem save)) return false;
            if (mission == null || missionManager == null || missionManager.Tracker == null) return false;

            var progress = save.Data.progress;
            if (progress.checkpointLevelId != mission.levelId || string.IsNullOrEmpty(progress.lastCheckpointId))
                return false;

            // یافتن Volume چک‌پوینت در صحنه برای موقعیت
            CheckpointVolume volume = FindVolume(progress.lastCheckpointId);
            if (volume == null)
            {
                GameLog.Warn($"[Checkpoint] Volume برای '{progress.lastCheckpointId}' پیدا نشد؛ چک‌پوینت نادیده گرفته شد.");
                return false;
            }

            _player.Teleport(volume.transform.position + Vector3.up * 0.1f, Quaternion.LookRotation(volume.transform.forward));
            missionManager.Tracker.RestoreBeforeIndex(progress.lastObjectiveIndex);
            return true;
        }

        private static CheckpointVolume FindVolume(string id)
        {
            var volumes = FindObjectsByType<CheckpointVolume>(FindObjectsSortMode.None);
            for (int i = 0; i < volumes.Length; i++)
                if (volumes[i].Id == id) return volumes[i];
            return null;
        }
    }

    /// <summary>حجم تریگر چک‌پوینت: ورود بازیکن = Capture.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CheckpointVolume : MonoBehaviour
    {
        [SerializeField] private string id = "cp1";

        /// <summary>شناسه چک‌پوینت.</summary>
        public string Id => id;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (ServiceLocator.TryGet(out CheckpointManager manager))
                manager.Capture(id);
        }
    }

    /// <summary>حجم تریگر هدف Reach: ورود بازیکن = NotifyReach.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ObjectiveReachVolume : MonoBehaviour
    {
        [SerializeField] private string objectiveId = "reach";

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (ServiceLocator.TryGet(out MissionManager mission))
                mission.NotifyReach(objectiveId);
        }
    }
}
