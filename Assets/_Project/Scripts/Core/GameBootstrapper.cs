using System.Collections;
using FogWalker.Controls;
using FogWalker.Localization;
using FogWalker.Optimization;
using FogWalker.Save;
using FogWalker.Settings;
using FogWalker.UI;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Core
{
    /// <summary>
    /// نقطه ورود واحد بازی؛ در صحنه Bootstrap قرار می‌گیرد.
    /// مسئولیت‌ها: ساخت/ثبت سرویس‌ها در ServiceLocator، ماندگارکردن آن‌ها بین صحنه‌ها،
    /// و هدایت بازی به MainMenu. هرگز منطق gameplay در این کلاس ننویسید.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper _instance;

        [Header("سیستم‌ها")]
        [SerializeField, Tooltip("پری‌فب حاوی سرویس‌ها (Input, Settings, Quality, Localization, SceneLoader, UI). طبق Docs/03 ساخته می‌شود.")]
        private GameObject systemsPrefab;

        private void Awake()
        {
            // جلوگیری از دوبار بوت‌شدن (مثلاً برگشت به صحنه Bootstrap)
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            // یک فریم صبر تا سیستم رندر/ورودی یونیتی آماده شود (پایداری روی دستگاه‌های کند).
            yield return null;
            Boot();
        }

        /// <summary>ترتیب ساخت سرویس‌ها مهم است: Save اول، UI/SceneLoader آخر.</summary>
        private void Boot()
        {
            GameLog.Info("[Boot] شروع راه‌اندازی FogWalker...");

            GameObject systems = systemsPrefab != null
                ? Instantiate(systemsPrefab)
                : new GameObject("Systems (Runtime)");
            DontDestroyOnLoad(systems);

            // ۱) ذخیره‌سازی — چون همه تنظیمات از آن خوانده می‌شود.
            var saveSystem = new SaveSystem();
            saveSystem.Load();
            ServiceLocator.Register<ISaveSystem>(saveSystem);

            // ۲) ورودی — GameStateManager برای دروازه ورودی به آن نیاز دارد.
            var inputManager = GetOrAdd<InputManager>(systems);
            ServiceLocator.Register(inputManager);

            // ۳) ماشین‌حالت — قلب کنترل جریان بازی.
            var stateManager = new GameStateManager(inputManager.SetGameplayInputEnabled);
            ServiceLocator.Register(stateManager);

            // ۴) کیفیت گرافیک → تنظیمات → زبان (SettingsManager برای زبان به Localization نیاز دارد).
            var qualityManager = GetOrAdd<QualityManager>(systems);
            ServiceLocator.Register(qualityManager);

            var localization = GetOrAdd<LocalizationManager>(systems);
            localization.Initialize(saveSystem);
            ServiceLocator.Register(localization);

            var settings = GetOrAdd<SettingsManager>(systems);
            settings.Initialize(saveSystem, qualityManager, localization);
            ServiceLocator.Register(settings);

            // ۵) صحنه و UI.
            var sceneLoader = GetOrAdd<SceneLoader>(systems);
            ServiceLocator.Register(sceneLoader);

            var uiManager = GetOrAdd<UIManager>(systems);
            ServiceLocator.Register(uiManager);

            // ۶) ورود به منوی اصلی.
            stateManager.SetState(GameState.MainMenu);
            sceneLoader.LoadMainMenu();

            GameLog.Info("[Boot] راه‌اندازی کامل شد.");
        }

        /// <summary>رفتن بازی به پس‌زمینه/قطع تماس: Save در حد امکان ذخیره شود.</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused && ServiceLocator.TryGet(out ISaveSystem save))
                save.Save();
        }

        /// <summary>خروج از بازی: Save نهایی.</summary>
        private void OnApplicationQuit()
        {
            if (ServiceLocator.TryGet(out ISaveSystem save))
                save.Save();
        }

        /// <summary>کامپوننت را از سیستم‌ها می‌گیرد؛ اگر سیم‌کشی پری‌فب ناقص باشد، نسخه پیش‌فرض می‌سازد و هشدار می‌دهد.</summary>
        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component == null)
            {
                GameLog.Warn($"[Boot] کامپوننت {typeof(T).Name} در پری‌فب Systems نبود؛ نسخه پیش‌فرض ساخته شد.");
                component = root.AddComponent<T>();
            }
            return component;
        }
    }
}
