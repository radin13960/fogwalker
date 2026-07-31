using System.Collections;
using FogWalker.UI;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FogWalker.Core
{
    /// <summary>
    /// بارگذاری ناهمگام صحنه‌ها با صفحه Loading، پیشرفت نرم و حداقل زمان نمایش.
    /// حالت بازی هنگام بارگذاری به Loading می‌رود تا ورودی ناخواسته مسدود شود.
    /// در فازهای بعدی، بارگذاری Additive مراحل به همین کلاس اضافه می‌شود.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        [Header("پیکربندی")]
        [SerializeField, Tooltip("کاتالوگ صحنه‌ها (Setup > 2)")]
        private SceneCatalog catalog;

        [SerializeField, Tooltip("حداقل ثانیه‌هایی که صفحه Loading دیده می‌شود تا چشمک نزند")]
        private float minLoadingSeconds = 0.8f;

        /// <summary>کاتالوگ صحنه‌ها برای انتخاب مرحله از منو.</summary>
        public SceneCatalog Catalog => catalog;

        /// <summary>آیا در حال بارگذاری هستیم؟ (برای جلوگیری از دابل‌کلیک)</summary>
        public bool IsLoading { get; private set; }

        /// <summary>بارگذاری منوی اصلی.</summary>
        public void LoadMainMenu()
        {
            if (catalog == null) { GameLog.Error("[SceneLoader] SceneCatalog اختصاص داده نشده است!"); return; }
            StartLoad(catalog.mainMenuScene, GameState.MainMenu);
        }

        /// <summary>بارگذاری یک مرحله با شناسه؛ اگر شناسه نامعتبر بود، به اولین مرحله می‌رود.</summary>
        public void LoadLevelById(string levelId)
        {
            if (catalog == null) { GameLog.Error("[SceneLoader] SceneCatalog اختصاص داده نشده است!"); return; }

            SceneCatalog.LevelEntry entry = catalog.GetById(levelId) ?? catalog.GetFirstLevel();
            if (entry == null)
            {
                GameLog.Error("[SceneLoader] هیچ مرحله‌ای در Catalog تعریف نشده است!");
                return;
            }

            var save = ServiceLocator.TryGet<Save.ISaveSystem>();
            if (save != null)
            {
                save.Data.progress.lastLevelId = entry.levelId;
                save.Data.progress.hasSave = true;
                save.Save();
            }

            StartLoad(entry.sceneName, GameState.Playing);
        }

        /// <summary>بارگذاری مجدد صحنه فعلی (برای «شروع مجدد مرحله»).</summary>
        public void RestartCurrentScene()
        {
            StartLoad(SceneManager.GetActiveScene().name, GameState.Playing);
        }

        private void StartLoad(string sceneName, GameState endState)
        {
            if (IsLoading)
            {
                GameLog.Warn($"[SceneLoader] بارگذاری دیگری در جریان است؛ درخواست '{sceneName}' نادیده گرفته شد.");
                return;
            }
            if (string.IsNullOrEmpty(sceneName))
            {
                GameLog.Error("[SceneLoader] نام صحنه خالی است!");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName, endState));
        }

        private IEnumerator LoadRoutine(string sceneName, GameState endState)
        {
            IsLoading = true;

            var stateManager = ServiceLocator.TryGet<GameStateManager>();
            stateManager?.SetState(GameState.Loading);

            var ui = ServiceLocator.TryGet<UIManager>();
            ui?.ShowLoading();
            ui?.SetLoadingProgress(0f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                GameLog.Error($"[SceneLoader] صحنه '{sceneName}' پیدا نشد؛ به Build Settings اضافه‌اش کنید.");
                ui?.HideLoading();
                IsLoading = false;
                stateManager?.SetState(GameState.MainMenu);
                yield break;
            }

            operation.allowSceneActivation = false;

            float elapsed = 0f;
            float displayed = 0f;
            while (!operation.isDone)
            {
                elapsed += Time.unscaledDeltaTime;

                // پیشرفت واقعی تا ۹۰٪ می‌رسد؛ ۱۰٪ پایانی پس از فعال‌سازی است. نمایش را نرم و محدود به حداقل زمان می‌کنیم.
                float target = Mathf.Clamp01(operation.progress / 0.9f);
                target = Mathf.Min(target, minLoadingSeconds <= 0f ? 1f : elapsed / minLoadingSeconds);
                displayed = Mathf.MoveTowards(displayed, target, Time.unscaledDeltaTime * 2f);
                ui?.SetLoadingProgress(displayed);

                if (operation.progress >= 0.9f && elapsed >= minLoadingSeconds && displayed >= 0.999f)
                    operation.allowSceneActivation = true;

                yield return null;
            }

            ui?.SetLoadingProgress(1f);
            yield return null; // یک فریم برای پایدارشدن صحنه جدید

            ui?.HideLoading();
            IsLoading = false;
            stateManager?.SetState(endState);

            GameLog.Info($"[SceneLoader] صحنه '{sceneName}' در {elapsed:0.00} ثانیه بارگذاری شد.");
        }
    }
}
