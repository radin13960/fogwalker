using FogWalker.Core;
using FogWalker.Save;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.UI.MainMenu
{
    /// <summary>
    /// کنترلر منوی اصلی: شروع بازی جدید (با انتخاب سختی)، ادامه، انتخاب مرحله، تنظیمات، خروج.
    /// همه دکمه‌ها در Inspector سیم می‌شوند؛ در غیاب سرویس‌ها (اجرای مستقیم صحنه بدون Bootstrap) خطای راهنما می‌دهد.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("پنل ریشه")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject difficultyPanel;

        [Header("دکمه‌های اصلی")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("انتخاب سختی")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button difficultyBackButton;

        [Header("پنل‌های فرزند")]
        [SerializeField] private LevelSelectPanel levelSelectPanel;
        [SerializeField] private Settings.SettingsPanel settingsPanel;

        private ISaveSystem _save;
        private SceneLoader _sceneLoader;

        private void Start()
        {
            if (!ResolveServices()) return;
            BindButtons();
            ShowMainPanel();
        }

        private void OnEnable()
        {
            // پس‌زدن از پنل‌ها (مثل بازنشانی Save در تنظیمات) وضعیت «ادامه» به‌روز شود.
            if (_save != null) RefreshContinueButton();
        }

        private bool ResolveServices()
        {
            bool ok = ServiceLocator.TryGet(out _save) & ServiceLocator.TryGet(out _sceneLoader);
            if (!ok)
                GameLog.Error("[MainMenu] سرویس‌ها آماده نیستند؛ صحنه را از Bootstrap اجرا کنید (مستند ۰۳).");
            return ok;
        }

        private void BindButtons()
        {
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);
            if (newGameButton != null) newGameButton.onClick.AddListener(ShowDifficultyPanel);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OpenLevelSelect);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (easyButton != null) easyButton.onClick.AddListener(() => StartNewGame(0));
            if (normalButton != null) normalButton.onClick.AddListener(() => StartNewGame(1));
            if (hardButton != null) hardButton.onClick.AddListener(() => StartNewGame(2));
            if (difficultyBackButton != null) difficultyBackButton.onClick.AddListener(ShowMainPanel);
        }

        // ---------- جریان منو ----------

        private void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (difficultyPanel != null) difficultyPanel.SetActive(false);
            RefreshContinueButton();
        }

        private void ShowDifficultyPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (difficultyPanel != null) difficultyPanel.SetActive(true);
        }

        private void RefreshContinueButton()
        {
            if (continueButton != null)
                continueButton.interactable = _save.Data.progress.hasSave;
        }

        /// <summary>شروع بازی جدید: پیشرفت قبلی پاک می‌شود ولی تنظیمات حفظ می‌ماند.</summary>
        private void StartNewGame(int difficultyIndex)
        {
            _save.ResetProgress(difficultyIndex);

            SceneCatalog.LevelEntry first = _sceneLoader.Catalog != null ? _sceneLoader.Catalog.GetFirstLevel() : null;
            if (first == null)
            {
                GameLog.Error("[MainMenu] هیچ مرحله‌ای در SceneCatalog نیست!");
                return;
            }
            _sceneLoader.LoadLevelById(first.levelId);
        }

        private void HandleContinue()
        {
            string levelId = _save.Data.progress.lastLevelId;
            _sceneLoader.LoadLevelById(levelId); // نامعتبر/خالی → SceneLoader خودش به مرحله اول می‌رود
        }

        private void OpenLevelSelect()
        {
            if (levelSelectPanel != null) levelSelectPanel.Open();
        }

        private void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void QuitGame()
        {
            GameLog.Info("[MainMenu] خروج از بازی.");
            _save.Save(); // اطمینان از پایداری آخرین تنظیمات
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
