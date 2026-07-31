using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.UI.Settings;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.UI.Menus
{
    /// <summary>
    /// منوی Pause درون‌بازی: ادامه، شروع از چک‌پوینت، شروع مجدد، تنظیمات، خروج به منو.
    /// ورود با InputManager.OnPauseRequested در GameplayBootstrapper مدیریت می‌شود؛ این کلاس فقط دکمه‌ها را می‌چسباند.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartCheckpointButton;
        [SerializeField] private Button restartLevelButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitToMenuButton;
        [SerializeField] private SettingsPanel settingsPanel;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => With(B => B.ResumeFromPause()));
            if (restartCheckpointButton != null) restartCheckpointButton.onClick.AddListener(() => With(B => B.ContinueFromCheckpoint()));
            if (restartLevelButton != null) restartLevelButton.onClick.AddListener(() => With(B => B.RestartLevel()));
            if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(() => With(B => B.QuitToMainMenu()));
            if (settingsButton != null) settingsButton.onClick.AddListener(() =>
            {
                if (settingsPanel != null) settingsPanel.Open();
            });
        }

        private void With(System.Action<GameplayBootstrapper> action)
        {
            var boot = FindFirstObjectByType<GameplayBootstrapper>();
            if (boot != null) action(boot);
            else GameLog.Warn("[Pause] GameplayBootstrapper در صحنه نیست.");
        }
    }

    /// <summary>
    /// صفحه شکست (مرگ بازیکن): ادامه از چک‌پوینت، شروع مجدد، منوی اصلی.
    /// هر سه خروجی این صفحه بازیکن را به مسیر روشنی می‌برند — هیچ Soft Lock وجود ندارد.
    /// </summary>
    public sealed class DeathScreen : MonoBehaviour
    {
        [SerializeField] private Button continueCheckpointButton;
        [SerializeField] private Button restartLevelButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (continueCheckpointButton != null) continueCheckpointButton.onClick.AddListener(() => With(B => B.ContinueFromCheckpoint()));
            if (restartLevelButton != null) restartLevelButton.onClick.AddListener(() => With(B => B.RestartLevel()));
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => With(B => B.QuitToMainMenu()));
        }

        private void With(System.Action<GameplayBootstrapper> action)
        {
            var boot = FindFirstObjectByType<GameplayBootstrapper>();
            if (boot != null) action(boot);
        }
    }

    /// <summary>
    /// صفحه پایان مرحله: مرحله بعدی (اگر باز شده)، تکرار، منوی اصلی.
    /// آمار با HUDController پر می‌شود؛ این کلاس فقط دکمه‌ها را مدیریت می‌کند.
    /// </summary>
    public sealed class LevelCompleteScreen : MonoBehaviour
    {
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(() => With(B => B.LoadNextLevel()));
            if (replayButton != null) replayButton.onClick.AddListener(() => With(B => B.ContinueFromCheckpoint()));
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => With(B => B.QuitToMainMenu()));
        }

        private void With(System.Action<GameplayBootstrapper> action)
        {
            var boot = FindFirstObjectByType<GameplayBootstrapper>();
            if (boot != null) action(boot);
        }

        /// <summary>پاک‌کردن چک‌پوینت قبل از تکرار مرحله (ازایند معرف «تکرار»).</summary>
        private void OnEnable()
        {
            // اگر مرحله تمام شده، چک‌پوینت معنایی ندارد؛ تکرار = شروع تازه
            var boot = FindFirstObjectByType<GameplayBootstrapper>();
            if (boot != null && ServiceLocator.TryGet(out Save.ISaveSystem save) &&
                ServiceLocator.TryGet(out Gameplay.Missions.MissionManager mm) && mm.Mission != null)
            {
                Gameplay.Missions.ProgressUnlocker.ClearCheckpoint(save.Data.progress, mm.Mission.levelId);
                save.Save();
            }
        }
    }
}
