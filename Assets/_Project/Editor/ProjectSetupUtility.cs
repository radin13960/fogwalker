using System.Collections.Generic;
using System.IO;
using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.Localization;
using FogWalker.Optimization;
using UnityEditor;
using UnityEngine;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// ابزار راه‌اندازی خودکار پروژه (منوی FogWalker در Editor):
    /// ۱) ساخت درخت پوشه‌ها طبق سند معماری
    /// ۲) ساخت ScriptableObjectهای پایه (Quality ×3، Difficulty ×3، SceneCatalog، Localization فارسی)
    /// تا کار دستی و خطای YAML حذف شود. Assetها در Assets/_Project/ScriptableObjects ذخیره می‌شوند.
    /// </summary>
    public static class ProjectSetupUtility
    {
        private const string Root = "Assets/_Project";
        private const string SoRoot = Root + "/ScriptableObjects";

        [MenuItem("FogWalker/Setup/1 - ساخت ساختار پوشه‌ها")]
        public static void CreateFolderStructure()
        {
            string[] folders =
            {
                "Art/Materials", "Art/Models", "Art/Textures", "Art/Animations", "Art/VFX", "Art/Audio", "Art/UI/Fonts",
                "Scenes/Bootstrap", "Scenes/MainMenu", "Scenes/Levels", "Scenes/Shared",
                "Prefabs/Player", "Prefabs/Enemies", "Prefabs/Weapons", "Prefabs/Environment", "Prefabs/UI", "Prefabs/VFX",
                "Scripts/Core/Events", "Scripts/Gameplay/Player", "Scripts/Gameplay/Weapons", "Scripts/Gameplay/AI",
                "Scripts/Gameplay/Missions", "Scripts/Gameplay/Combat", "Scripts/Gameplay/Interactions",
                "Scripts/UI/MainMenu", "Scripts/UI/Settings", "Scripts/UI/Common",
                "Scripts/Audio", "Scripts/Save", "Scripts/Optimization", "Scripts/Utilities", "Scripts/Controls", "Scripts/Localization",
                "ScriptableObjects/Weapons", "ScriptableObjects/Enemies", "ScriptableObjects/Missions",
                "ScriptableObjects/Difficulty", "ScriptableObjects/Audio", "ScriptableObjects/Localization",
                "ScriptableObjects/Quality", "ScriptableObjects/Scenes",
                "Settings/Input", "Addressables", "Tests/EditMode", "Tests/PlayMode",
                "../ThirdParty"
            };

            int created = 0;
            foreach (string folder in folders)
            {
                string path = folder.StartsWith("..") ? "Assets/ThirdParty" : Root + "/" + folder;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    created++;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[FogWalker Setup] ساختار پوشه‌ها آماده شد ({created} پوشه جدید).");
        }

        [MenuItem("FogWalker/Setup/2 - ساخت ScriptableObjectهای پایه")]
        public static void CreateScriptableObjects()
        {
            CreateQualityProfiles();
            CreateDifficultyProfiles();
            CreateDifficultyLibrary();
            CreatePlayerTuning();
            CreateSceneCatalog();
            CreateLocalizationTableFa();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FogWalker Setup] ScriptableObjectهای پایه ساخته شدند.\n" +
                      "گام بعد: طبق Docs/03 آن‌ها را به QualityManager، SceneLoader و LocalizationManager در پری‌فب Systems متصل کنید.");
        }

        // ---------- Quality ----------

        private static void CreateQualityProfiles()
        {
            CreateOrReplaceAsset($"{SoRoot}/Quality/QP_Performance.asset", (QualityProfileSO p) =>
            {
                p.profileName = "Performance";
                p.renderScale = 0.75f; p.msaaSampleCount = 1; p.hdr = true;
                p.mainLightShadowResolution = 1024; p.shadowDistance = 35f; p.shadowCascades = 1;
                p.pixelLightCount = 2; p.lodBias = 0.7f; p.textureMipmapLimit = 1;
                p.postProcessing = false; p.particleBudget = 60; p.drawDistance = 100f;
            });

            CreateOrReplaceAsset($"{SoRoot}/Quality/QP_Balanced.asset", (QualityProfileSO p) =>
            {
                p.profileName = "Balanced";
                p.renderScale = 0.85f; p.msaaSampleCount = 2; p.hdr = true;
                p.mainLightShadowResolution = 2048; p.shadowDistance = 60f; p.shadowCascades = 2;
                p.pixelLightCount = 3; p.lodBias = 1f; p.textureMipmapLimit = 0;
                p.postProcessing = true; p.particleBudget = 100; p.drawDistance = 150f;
            });

            CreateOrReplaceAsset($"{SoRoot}/Quality/QP_High.asset", (QualityProfileSO p) =>
            {
                p.profileName = "High";
                p.renderScale = 1f; p.msaaSampleCount = 4; p.hdr = true;
                p.mainLightShadowResolution = 2048; p.shadowDistance = 90f; p.shadowCascades = 3;
                p.pixelLightCount = 4; p.lodBias = 1.5f; p.textureMipmapLimit = 0;
                p.postProcessing = true; p.particleBudget = 200; p.drawDistance = 220f;
            });
        }

        // ---------- Difficulty ----------

        private static void CreateDifficultyProfiles()
        {
            CreateOrReplaceAsset($"{SoRoot}/Difficulty/DS_Easy.asset", (DifficultySettingsSO d) =>
            {
                d.difficultyIndex = 0; d.displayNameKey = "difficulty.easy";
                d.enemyHealthMultiplier = 0.8f; d.enemyDamageMultiplier = 0.7f;
                d.enemyBaseAccuracy = 0.4f; d.enemyReactionScale = 0.8f;
                d.ammoPickupMultiplier = 1.5f; d.medkitPickupMultiplier = 1.5f;
            });

            CreateOrReplaceAsset($"{SoRoot}/Difficulty/DS_Normal.asset", (DifficultySettingsSO d) =>
            {
                d.difficultyIndex = 1; d.displayNameKey = "difficulty.normal";
                d.enemyHealthMultiplier = 1f; d.enemyDamageMultiplier = 1f;
                d.enemyBaseAccuracy = 0.55f; d.enemyReactionScale = 1f;
                d.ammoPickupMultiplier = 1f; d.medkitPickupMultiplier = 1f;
            });

            CreateOrReplaceAsset($"{SoRoot}/Difficulty/DS_Hard.asset", (DifficultySettingsSO d) =>
            {
                d.difficultyIndex = 2; d.displayNameKey = "difficulty.hard";
                d.enemyHealthMultiplier = 1.25f; d.enemyDamageMultiplier = 1.3f;
                d.enemyBaseAccuracy = 0.7f; d.enemyReactionScale = 1.2f;
                d.ammoPickupMultiplier = 0.7f; d.medkitPickupMultiplier = 0.7f;
            });
        }

        // ---------- Scenes ----------

        private static void CreateSceneCatalog()
        {
            CreateOrReplaceAsset($"{SoRoot}/Scenes/SceneCatalog_Main.asset", (SceneCatalog c) =>
            {
                c.bootstrapScene = "Bootstrap";
                c.mainMenuScene = "MainMenu";
                c.levels = new[]
                {
                    new SceneCatalog.LevelEntry { levelId = "level1", sceneName = "Level1_Boulevard", displayNameKey = "level.1.name" },
                    new SceneCatalog.LevelEntry { levelId = "level2", sceneName = "Level2_Bazaar",    displayNameKey = "level.2.name" },
                    new SceneCatalog.LevelEntry { levelId = "level3", sceneName = "Level3_Bridge",    displayNameKey = "level.3.name" },
                };
            });
        }

        // ---------- کتابخانه سختی و تنظیمات بازیکن ----------

        private static readonly string[] DifficultyAssetPaths =
            { SoRoot + "/Difficulty/DS_Easy.asset", SoRoot + "/Difficulty/DS_Normal.asset", SoRoot + "/Difficulty/DS_Hard.asset" };

        private static void CreateDifficultyLibrary()
        {
            CreateOrReplaceAsset(SoRoot + "/Difficulty/DifficultyLibrary.asset", (DifficultyLibrarySO lib) =>
            {
                lib.difficulties = new DifficultySettingsSO[3];
                for (int i = 0; i < 3; i++)
                    lib.difficulties[i] = AssetDatabase.LoadAssetAtPath<DifficultySettingsSO>(DifficultyAssetPaths[i]);
            });
        }

        private static void CreatePlayerTuning()
        {
            CreateOrReplaceAsset(SoRoot + "/Player/PlayerTuning_Main.asset", (FogWalker.Gameplay.Player.PlayerTuningSO t) => { /* پیش‌فرض‌ها کافی‌اند */ });
        }

        // ---------- Localization (فارسی) ----------

        private static void CreateLocalizationTableFa()
        {
            var fa = new (string key, string text)[]
            {
                ("game.title", "مه‌نورد"),
                ("menu.continue", "ادامه بازی"),
                ("menu.new_game", "شروع بازی جدید"),
                ("menu.level_select", "انتخاب مرحله"),
                ("menu.settings", "تنظیمات"),
                ("menu.quit", "خروج"),
                ("menu.difficulty_title", "انتخاب درجه سختی"),
                ("difficulty.easy", "آسان"),
                ("difficulty.normal", "عادی"),
                ("difficulty.hard", "سخت"),
                ("common.back", "بازگشت"),
                ("common.yes", "بله"),
                ("common.no", "خیر"),
                ("common.locked", "قفل"),
                ("loading.hint", "در حال بارگذاری... لطفاً صبر کنید"),
                ("settings.title", "تنظیمات"),
                ("settings.quality", "کیفیت گرافیک"),
                ("settings.quality.performance", "بهره‌وری"),
                ("settings.quality.balanced", "متعادل"),
                ("settings.quality.high", "بالا"),
                ("settings.fps", "نرخ فریم هدف"),
                ("settings.volume_master", "صدای کلی"),
                ("settings.volume_music", "موسیقی"),
                ("settings.volume_sfx", "افکت‌های صوتی"),
                ("settings.sensitivity", "حساسیت دوربین"),
                ("settings.invert_y", "معکوس‌کردن محور عمودی"),
                ("settings.camera_shake", "لرزش دوربین"),
                ("settings.haptics", "لرزش لمسی"),
                ("settings.left_handed", "چیدمان چپ‌دست"),
                ("settings.control_size", "اندازه کنترل‌ها"),
                ("settings.control_opacity", "شفافیت کنترل‌ها"),
                ("settings.reset_save", "بازنشانی پیشرفت"),
                ("settings.reset_confirm", "همه پیشرفت پاک شود؟ این کار بازگشت‌ناپذیر است."),
                ("level.select_title", "انتخاب مرحله"),
                ("level.1.name", "بلوار خاموش"),
                ("level.2.name", "بازارچه متروک"),
                ("level.3.name", "پل مه‌آلود"),
                // ---- گیم‌پلی (فاز ۲+) ----
                ("pause.title", "توقف بازی"),
                ("pause.resume", "ادامه"),
                ("pause.restart_checkpoint", "شروع از آخرین چک‌پوینت"),
                ("pause.restart_level", "شروع مجدد مرحله"),
                ("pause.settings", "تنظیمات"),
                ("pause.main_menu", "بازگشت به منوی اصلی"),
                ("death.title", "مأموریت ناموفق بود"),
                ("death.continue", "ادامه از آخرین چک‌پوینت"),
                ("death.restart", "شروع مجدد مرحله"),
                ("death.menu", "منوی اصلی"),
                ("complete.title", "مرحله تکمیل شد"),
                ("complete.next", "مرحله بعدی"),
                ("complete.replay", "تکرار مرحله"),
                ("complete.menu", "منوی اصلی"),
                ("complete.time", "زمان"),
                ("complete.accuracy", "دقت"),
                ("complete.kills", "دشمنان حذف‌شده"),
                ("complete.objectives", "اهداف انجام‌شده"),
                ("complete.reward", "مرحله جدید باز شد"),
                ("hud.interact", "تعامل"),
                ("hud.cover", "کاور"),
                ("hud.checkpoint_saved", "چک‌پوینت ذخیره شد"),
                ("hud.wave_incoming", "موج دشمن در راه است!"),
                ("hud.defend", "از ناحیه دفاع کنید"),
                ("hud.extract", "به نقطه خروج برسید"),
                ("hud.grenade", "نارنجک"),
                ("hud.medkit_used", "سلامتی بازیابی شد"),
                ("hud.ammo_empty", "خشاب خالی است"),
                ("settings.auto_quality", "کیفیت تطبیقی خودکار"),
                // ---- اهداف مرحله ۱: بلوار خاموش ----
                ("obj.l1.1", "به نقطه امن برسید"),
                ("obj.l1.2", "مهاجمان را از بین ببرید"),
                ("obj.l1.3", "از کاور استفاده کنید و پیش بروید"),
                ("obj.l1.4", "غیرنظامی را نجات دهید"),
                ("obj.l1.5", "به نقطه استخراج برسید"),
                // ---- اهداف مرحله ۲: بازارچه متروک ----
                ("obj.l2.1", "سه منبع انرژی را پیدا کنید"),
                ("obj.l2.2", "مسیر را باز کنید"),
                ("obj.l2.3", "از نقطه دفاع کنید"),
                ("obj.l2.4", "از منطقه خارج شوید"),
                // ---- اهداف مرحله ۳: پل مه‌آلود ----
                ("obj.l3.1", "از پل عبور کنید"),
                ("obj.l3.2", "نیروهای مقاوم را متوقف کنید"),
                ("obj.l3.3", "مسیر خروج را آزاد کنید"),
                ("obj.l3.4", "دفاع نهایی"),
                // ---- دکمه‌های لمسی HUD ----
                ("hud.fire", "آتش"),
                ("hud.aim", "هدف"),
                ("hud.reload", "تعویض خشاب"),
                ("hud.weapon", "سلاح"),
                ("hud.jump", "پرش"),
                ("hud.crouch", "خمیدن"),
                ("hud.sprint", "تاختن"),
                ("hud.pause", "توقف"),
                ("weapon.pistol", "تپانچه"),
                ("weapon.ar", "تفنگ تهاجمی"),
                ("weapon.smg", "مسلسل سبک"),
                ("weapon.shotgun", "شاتگان"),
                ("weapon.dmr", "تفنگ نشانه‌زن"),
            };

            CreateOrReplaceAsset($"{SoRoot}/Localization/LocTable_FA.asset", (LocalizationTableSO table) =>
            {
                table.entries.Clear();
                foreach ((string key, string text) in fa)
                    table.entries.Add(new LocalizationEntry { key = key, fa = text, en = key });
                table.BuildLookup();
            });
        }

        // ---------- کمکی ----------

        private static void CreateOrReplaceAsset<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            EnsureFolder(Path.GetDirectoryName(path));

            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool isNew = asset == null;
            if (isNew)
                asset = ScriptableObject.CreateInstance<T>();

            configure(asset);

            if (isNew)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);

            Debug.Log($"[FogWalker Setup] {(isNew ? "ساخته شد: " : "به‌روز شد: ")} {path}");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || Directory.Exists(folderPath)) return;
            Directory.CreateDirectory(folderPath);
        }
    }
}
