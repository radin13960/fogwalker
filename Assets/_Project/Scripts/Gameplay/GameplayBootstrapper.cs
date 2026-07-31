using FogWalker.Audio;
using FogWalker.Controls;
using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Player;
using FogWalker.Gameplay.Weapons;
using FogWalker.Optimization;
using FogWalker.Save;
using FogWalker.UI;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay
{
    /// <summary>
    /// چسب مرحله: پیکربندی سختی، اتصال سرویس‌های سطح-مرحله به Locator، Spawn بازیکن در نقطه/چک‌پوینت،
    /// رویدادهای مرگ/پایان/توقف، و تمیزکاری هنگام خروج. هر Scene مرحله یکی از این‌ها دارد.
    /// </summary>
    public sealed class GameplayBootstrapper : MonoBehaviour
    {
        [Header("داده‌های مشترک")]
        [SerializeField] private DifficultyLibrarySO difficultyLibrary;
        [SerializeField] private GrenadeDataSO grenadeData;
        [SerializeField, Tooltip("داده ۵ سلاح به ترتیب Pistol, AssaultRifle, SMG, Shotgun, DMR — دو تای اولی به بازیکن داده می‌شود")]
        private WeaponDataSO[] startingWeapons;
        [SerializeField] private ImpactLibrarySO impactLibrary;

        [Header("بازیکن (نمونه موجود در صحنه)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform playerSpawnPoint;

        private bool _deathHandled;

        /// <summary>آمار پایان مرحله برای صفحه نتایج (خواندن از LevelCompleteScreen).</summary>
        public MissionStats LastMissionStats { get; private set; }

        private void Awake()
        {
            // سختی فعال
            if (ServiceLocator.TryGet(out ISaveSystem save) && difficultyLibrary != null)
                DifficultyContext.Current = difficultyLibrary.Get(save.Data.progress.difficulty);
            else
                DifficultyContext.Current = difficultyLibrary != null ? difficultyLibrary.Get(1) : null;
        }

        private void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            if (player == null)
            {
                GameLog.Error("[Level] بازیکن در صحنه پیدا نشد! پری‌فب Player را بگذارید.");
                return;
            }

            // ثبت سرویس‌های مرحله
            if (TryGetComponent(out MissionManager missionManager)) ServiceLocator.Register(missionManager);
            if (TryGetComponent(out CheckpointManager checkpointManager)) ServiceLocator.Register(checkpointManager);
            if (TryGetComponent(out SpawnManager spawnManager)) { spawnManager.PlayerTransform = player.transform; ServiceLocator.Register(spawnManager); }
            if (!ServiceLocator.TryGet(out AudioManager _) && FindFirstObjectByType<AudioManager>() is AudioManager am) ServiceLocator.Register(am);
            if (FindFirstObjectByType<PoolManager>() is PoolManager pool && !ServiceLocator.TryGet(out PoolManager _)) ServiceLocator.Register(pool);

            ImpactLibrarySource.Library = impactLibrary;
            Player.Controllers.CameraShaker.RegisterPlayer(player.transform);

            // پیکربندی بازیکن
            ConfigurePlayer();

            // بازیابی از چک‌پوینت در صورت وجود
            if (ServiceLocator.TryGet(out CheckpointManager ck) && ServiceLocator.TryGet(out MissionManager mm))
                ck.ApplySavedCheckpointIfAny(player, mm);

            // رویدادها
            var health = player.GetComponent<HealthComponent>();
            if (health != null) health.OnDied += HandlePlayerDied;
            if (ServiceLocator.TryGet(out MissionManager mm2))
                mm2.OnMissionCompleted += HandleMissionCompleted;

            // Pause با دکمه
            if (ServiceLocator.TryGet(out InputManager input))
                input.OnPauseRequested += HandlePauseRequested;

            Audio.AudioManager.SetMoodShielded(MusicMood.Exploration);

            // موسیقی تغییر با آگاهی/درگیری: فقط وقتی دشمن فعال است — ساده: Combat هنگام شلیک بازیکن گزارش شد
            DamageEvents.OnDamaged += HandleCombatSignal;
            AISoundBus.OnSound += HandleSoundSignal;
        }

        private void ConfigurePlayer()
        {
            var health = player.GetComponent<HealthComponent>();
            var tuning = player.Tuning;
            if (health != null && tuning != null)
                health.Initialize(tuning.maxHealth, tuning.regenPerSecond, tuning.regenDelay);

            var inventory = player.GetComponent<WeaponInventory>();
            var cameraCtrl = player.GetComponent<PlayerCameraController>();
            if (inventory != null && cameraCtrl != null && cameraCtrl.MainCamera != null)
            {
                inventory.Initialize(cameraCtrl.MainCamera);
                if (startingWeapons != null)
                {
                    foreach (var w in startingWeapons)
                        if (w != null) inventory.AddWeapon(w, null);
                }
            }

            var grenades = player.GetComponent<GrenadeThrower>();
            if (grenades != null && tuning != null)
                grenades.Initialize(grenadeData, tuning.grenadeStartCount, tuning.grenadeMaxCount);
        }

        // ---------- Pause ----------

        private void HandlePauseRequested()
        {
            if (!ServiceLocator.TryGet(out GameStateManager state)) return;
            if (state.Current == GameState.Playing)
            {
                state.SetState(GameState.Paused);
                if (ServiceLocator.TryGet(out HUDController hud)) hud.ShowPause(true);
            }
            else if (state.Current == GameState.Paused)
            {
                ResumeFromPause();
            }
        }

        /// <summary>ادامه از Pause (دکمه Resume همین را صدا می‌زند).</summary>
        public void ResumeFromPause()
        {
            if (!ServiceLocator.TryGet(out GameStateManager state)) return;
            if (state.Current != GameState.Paused) return;
            if (ServiceLocator.TryGet(out HUDController hud)) hud.ShowPause(false);
            state.SetState(GameState.Playing);
        }

        // ---------- مرگ و پایان ----------

        private void HandlePlayerDied(DamageInfo info)
        {
            if (_deathHandled) return;
            _deathHandled = true;

            if (ServiceLocator.TryGet(out ISaveSystem save))
            {
                save.Data.stats.totalDeaths++;
                save.Save();
            }

            if (ServiceLocator.TryGet(out GameStateManager state))
                state.SetState(GameState.PlayerDead);

            if (ServiceLocator.TryGet(out HUDController hud)) hud.ShowDeath(true);
            Audio.AudioManager.SetMoodShielded(MusicMood.Tension);
        }

        private void HandleMissionCompleted(MissionStats stats)
        {
            LastMissionStats = stats;

            if (ServiceLocator.TryGet(out SpawnManager spawner)) spawner.DespawnAll();
            if (ServiceLocator.TryGet(out GameStateManager state))
                state.SetState(GameState.LevelComplete);
            if (ServiceLocator.TryGet(out HUDController hud)) hud.ShowLevelComplete(true, stats);
        }

        // ---------- اقدامات UI جریان بازی ----------

        /// <summary>ادامه از آخرین چک‌پوینت: Reload صحنه (چک‌پوینت در Start اعمال می‌شود).</summary>
        public void ContinueFromCheckpoint()
        {
            ReloadCurrentLevel();
        }

        /// <summary>شروع مجدد مرحله: پاک کردن چک‌پوینت این مرحله + Reload.</summary>
        public void RestartLevel()
        {
            if (ServiceLocator.TryGet(out ISaveSystem save) && ServiceLocator.TryGet(out MissionManager mm) && mm.Mission != null)
            {
                ProgressUnlocker.ClearCheckpoint(save.Data.progress, mm.Mission.levelId);
                save.Save();
            }
            ReloadCurrentLevel();
        }

        /// <summary>رفتن به منوی اصلی.</summary>
        public void QuitToMainMenu()
        {
            if (ServiceLocator.TryGet(out SceneLoader loader)) loader.LoadMainMenu();
        }

        /// <summary>بارگذاری مرحله بعد (از صفحه پایان).</summary>
        public void LoadNextLevel()
        {
            var next = LastMissionStats.UnlockedNextLevelId;
            if (ServiceLocator.TryGet(out SceneLoader loader))
            {
                if (!string.IsNullOrEmpty(next)) loader.LoadLevelById(next);
                else loader.LoadMainMenu();
            }
        }

        private void ReloadCurrentLevel()
        {
            if (ServiceLocator.TryGet(out SceneLoader loader)) loader.RestartCurrentScene();
        }

        // ---------- موسیقی پویا ساده ----------

        private float _combatMusicTimer;

        private void HandleCombatSignal(Component target, DamageInfo info) => PingCombat();
        private void HandleSoundSignal(Vector3 pos, float radius, float loud) { if (loud >= 0.75f) PingCombat(); }

        private void PingCombat()
        {
            _combatMusicTimer = 6f;
            Audio.AudioManager.SetMoodShielded(MusicMood.Combat);
        }

        private void Update()
        {
            if (_combatMusicTimer > 0f)
            {
                _combatMusicTimer -= Time.deltaTime;
                if (_combatMusicTimer <= 0f)
                    Audio.AudioManager.SetMoodShielded(MusicMood.Exploration);
            }
        }

        private void OnDestroy()
        {
            DamageEvents.OnDamaged -= HandleCombatSignal;
            AISoundBus.OnSound -= HandleSoundSignal;

            if (player != null)
            {
                var health = player.GetComponent<HealthComponent>();
                if (health != null) health.OnDied -= HandlePlayerDied;
            }
            if (ServiceLocator.TryGet(out MissionManager mm) && mm != null)
                mm.OnMissionCompleted -= HandleMissionCompleted;
            if (ServiceLocator.TryGet(out InputManager input) && input != null)
                input.OnPauseRequested -= HandlePauseRequested;

            // تمیزکاری سرویس‌های سطح-مرحله (بدون مرجع سرگردان)
            ServiceLocator.Unregister<MissionManager>();
            ServiceLocator.Unregister<CheckpointManager>();
            ServiceLocator.Unregister<SpawnManager>();
            Time.timeScale = 1f;
        }
    }
}
