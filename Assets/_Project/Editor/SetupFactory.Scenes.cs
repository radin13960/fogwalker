using System.Collections.Generic;
using System.IO;
using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.Gameplay.AI;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Interactions;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Player;
using FogWalker.Gameplay.Weapons;
using FogWalker.UI.HUD;
using FogWalker.UI.MainMenu;
using FogWalker.UI.Settings;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// بخش صحنه‌ها: HUD لمسی کامل، Bootstrap، MainMenu و سه مرحله آماده‌به‌بازی از primitiveها
    /// (Placeholder صریح با معماری کامل برای جایگزینی آرت نهایی).
    /// </summary>
    public static partial class SetupFactory
    {
        // ---------- ادامه HUD: کنترل‌های لمسی ----------

        private static void BuildHudTouchControls(RectTransform safe, HUDController hud)
        {
            var controlsRoot = NewRect("ControlsRoot", safe, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            controlsRoot.offsetMin = Vector2.zero; controlsRoot.offsetMax = Vector2.zero;
            var controlsGroup = controlsRoot.gameObject.AddComponent<CanvasGroup>();
            controlsGroup.interactable = true;
            controlsGroup.blocksRaycasts = true;

            // گروه چپ: جوی‌استیک
            var leftGroup = NewRect("LeftControls", controlsRoot, new Vector2(180f, 180f), new Vector2(400, 400),
                new Vector2(0f, 0f), new Vector2(0f, 0f));

            var joyBg = NewRect("Joystick", leftGroup, Vector2.zero, new Vector2(220, 220));
            NewImage(joyBg, UiColors.Touch, CircleSprite);
            var joyHandle = NewRect("Handle", joyBg, Vector2.zero, new Vector2(90, 90));
            NewImage(joyHandle, new Color(1f, 1f, 1f, 0.65f), CircleSprite);
            var joy = joyBg.gameObject.AddComponent<VirtualJoystick>();
            SetField(joy, "background", joyBg);
            SetField(joy, "handle", joyHandle);
            SetField(joy, "radiusPixels", 90f);

            // گروه راست: خوشه دکمه‌ها
            var rightGroup = NewRect("RightControls", controlsRoot, new Vector2(-180f, 180f), new Vector2(560, 500),
                new Vector2(1f, 0f), new Vector2(1f, 0f));

            MakeTouchCircle(rightGroup, "Fire", new Vector2(0f, 0f), 130f, "hud.fire", TouchHoldButton.Kind.Fire);
            MakeTouchCircle(rightGroup, "Aim", new Vector2(-170f, -30f), 100f, "hud.aim", TouchHoldButton.Kind.Aim);
            MakeTouchCircle(rightGroup, "Sprint", new Vector2(-40f, 220f), 90f, "hud.sprint", TouchHoldButton.Kind.Sprint);
            MakeTouchCircle(rightGroup, "Reload", new Vector2(-160f, 150f), 90f, "hud.reload", TouchPressButton.Kind.Reload);
            MakeTouchCircle(rightGroup, "Weapon", new Vector2(-30f, 120f), 90f, "hud.weapon", TouchPressButton.Kind.WeaponNext);
            MakeTouchCircle(rightGroup, "Grenade", new Vector2(-280f, 80f), 90f, "hud.grenade", TouchPressButton.Kind.Grenade);
            MakeTouchCircle(rightGroup, "Jump", new Vector2(-280f, -140f), 95f, "hud.jump", TouchPressButton.Kind.Jump);
            MakeTouchCircle(rightGroup, "Crouch", new Vector2(-150f, -190f), 95f, "hud.crouch", TouchPressButton.Kind.Crouch);
            MakeTouchCircle(rightGroup, "Cover", new Vector2(-280f, 230f), 90f, "hud.cover", TouchPressButton.Kind.Cover);
            MakeTouchCircle(rightGroup, "Interact", new Vector2(-410f, 0f), 95f, "hud.interact", TouchPressButton.Kind.Interact);

            // دکمه Pause بالای صفحه (راست)
            var pauseRt = NewRect("PauseBtn", safe, new Vector2(-70f, -60f), new Vector2(70, 70),
                new Vector2(1f, 1f), new Vector2(1f, 1f));
            NewImage(pauseRt, UiColors.TouchBtn, null);
            NewText("Lbl", pauseRt, Vector2.zero, new Vector2(70, 70), "❚❚", 30f, UiColors.Text, localize: false);
            pauseRt.gameObject.AddComponent<PauseButtonProxy>();

            SetField(hud, "leftControlsGroup", leftGroup);
            SetField(hud, "rightControlsGroup", rightGroup);
            SetField(hud, "controlsCanvasGroup", controlsGroup);
        }

        private static void MakeTouchCircle(RectTransform parent, string name, Vector2 pos, float size,
            string labelKey, TouchHoldButton.Kind holdKind)
        {
            var rt = NewRect(name, parent, pos, new Vector2(size, size));
            var img = NewImage(rt, UiColors.TouchBtn, CircleSprite);
            NewText("Lbl", rt, Vector2.zero, new Vector2(size, size), labelKey, 22f, UiColors.Text);
            var btn = rt.gameObject.AddComponent<TouchHoldButton>();
            SetField(btn, "kind", holdKind);
            SetField(btn, "visual", img);
        }

        private static void MakeTouchCircle(RectTransform parent, string name, Vector2 pos, float size,
            string labelKey, TouchPressButton.Kind pressKind)
        {
            var rt = NewRect(name, parent, pos, new Vector2(size, size));
            NewImage(rt, UiColors.TouchBtn, CircleSprite);
            NewText("Lbl", rt, Vector2.zero, new Vector2(size, size), labelKey, 22f, UiColors.Text);
            var btn = rt.gameObject.AddComponent<TouchPressButton>();
            SetField(btn, "kind", pressKind);
        }

        // ---------- HUD: پنل‌های Pause/Death/Complete ----------

        private static void BuildHudPanels(RectTransform safe, HUDController hud)
        {
            // پنل Pause
            var pauseRoot = NewDialogPanel("PausePanel", safe, new Vector2(760, 720));
            var pausePanelComp = pauseRoot.gameObject.AddComponent<PauseMenuController>();
            RectTransform pausePanel = (RectTransform)pauseRoot.Find("PausePanel_Panel");
            NewText("Title", pausePanel, new Vector2(0, 300f), new Vector2(700, 60), "pause.title", 34f, UiColors.Text);
            var resume = NewButton("Resume", pausePanel, new Vector2(0, 190f), new Vector2(520, 74), "pause.resume");
            var restartCk = NewButton("RestartCk", pausePanel, new Vector2(0, 95f), new Vector2(520, 74), "pause.restart_checkpoint");
            var restartLv = NewButton("RestartLv", pausePanel, new Vector2(0, 0f), new Vector2(520, 74), "pause.restart_level");
            var settingsBtn = NewButton("Settings", pausePanel, new Vector2(0, -95f), new Vector2(520, 74), "pause.settings");
            var quitBtn = NewButton("Quit", pausePanel, new Vector2(0, -190f), new Vector2(520, 74), "pause.main_menu");
            var hudSettingsPanel = BuildSettingsPanelContent(pauseRoot, startHidden: true);
            SetField(pausePanelComp, "resumeButton", resume);
            SetField(pausePanelComp, "restartCheckpointButton", restartCk);
            SetField(pausePanelComp, "restartLevelButton", restartLv);
            SetField(pausePanelComp, "settingsButton", settingsBtn);
            SetField(pausePanelComp, "quitToMenuButton", quitBtn);
            SetField(pausePanelComp, "settingsPanel", hudSettingsPanel);
            pauseRoot.gameObject.SetActive(false);
            SetField(hud, "pausePanel", pauseRoot.gameObject);

            // پنل مرگ
            var deathRoot = NewDialogPanel("DeathPanel", safe, new Vector2(760, 560));
            var deathComp = deathRoot.gameObject.AddComponent<DeathScreen>();
            RectTransform deathPanel = (RectTransform)deathRoot.Find("DeathPanel_Panel");
            NewText("Title", deathPanel, new Vector2(0, 230f), new Vector2(700, 60), "death.title", 34f, UiColors.Text);
            var contBtn = NewButton("Continue", deathPanel, new Vector2(0, 110f), new Vector2(560, 78), "death.continue");
            var restartBtn = NewButton("Restart", deathPanel, new Vector2(0, 0f), new Vector2(560, 78), "death.restart");
            var menuBtn = NewButton("Menu", deathPanel, new Vector2(0, -110f), new Vector2(560, 78), "death.menu");
            SetField(deathComp, "continueCheckpointButton", contBtn);
            SetField(deathComp, "restartLevelButton", restartBtn);
            SetField(deathComp, "mainMenuButton", menuBtn);
            deathRoot.gameObject.SetActive(false);
            SetField(hud, "deathPanel", deathRoot.gameObject);

            // پنل پایان مرحله
            var compRoot = NewDialogPanel("CompletePanel", safe, new Vector2(800, 720));
            var compComp = compRoot.gameObject.AddComponent<LevelCompleteScreen>();
            RectTransform compPanel = (RectTransform)compRoot.Find("CompletePanel_Panel");
            NewText("Title", compPanel, new Vector2(0, 300f), new Vector2(700, 60), "complete.title", 36f, new Color(0.4f, 0.9f, 0.4f));
            var statsText = NewText("Stats", compPanel, new Vector2(0, 160f), new Vector2(640, 160),
                "-", 28f, UiColors.Text, false, TextAlignmentOptions.Right);
            SetField(hud, "completeStatsText", statsText);
            var rewardText = NewText("Reward", compPanel, new Vector2(0, 60f), new Vector2(640, 50),
                "complete.reward", 26f, new Color(0.45f, 0.85f, 1f));
            SetField(hud, "completeRewardText", rewardText);
            var nextBtn = NewButton("Next", compPanel, new Vector2(0, -60f), new Vector2(560, 78), "complete.next");
            var replayBtn = NewButton("Replay", compPanel, new Vector2(0, -170f), new Vector2(560, 78), "complete.replay");
            var compMenuBtn = NewButton("Menu", compPanel, new Vector2(0, -280f), new Vector2(560, 78), "complete.menu");
            SetField(compComp, "nextLevelButton", nextBtn);
            SetField(compComp, "replayButton", replayBtn);
            SetField(compComp, "mainMenuButton", compMenuBtn);
            compRoot.gameObject.SetActive(false);
            SetField(hud, "completePanel", compRoot.gameObject);
        }

        // ---------- ساخت محتوای پنل تنظیمات (مشترک بین MainMenu و Pause) ----------

        /// <summary>ساخت محتوای کامل پنل تنظیمات و برگرداندن کامپوننت سیم‌شده SettingsPanel.</summary>
        public static SettingsPanel BuildSettingsPanelContent(Transform parent, bool startHidden)
        {
            var root = NewRect("SettingsRoot", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;
            var rt = root;

            var dimImg = NewImage(rt, new Color(0f, 0f, 0f, 0.78f));
            var panel = NewRect("Panel", rt, Vector2.zero, new Vector2(1150, 860));
            NewImage(panel, UiColors.Panel);
            // نکته: کامپوننت روی «روت» است تا Open/Close کل پنل (با Dim) را فعال/غیرفعال کند.
            var comp = root.gameObject.AddComponent<SettingsPanel>();

            NewText("Title", panel, new Vector2(0, 380f), new Vector2(800, 56), "settings.title", 34f, UiColors.Text);

            // ردیف‌ها: برچسب راست، ویجت چپ
            float x_label = 400f, x_widget = -140f;
            float y = 310f, dy = 76f;

            NewText("L1", panel, new Vector2(x_label, y), new Vector2(300, 50), "settings.quality", 24f, UiColors.Text, true, TextAlignmentOptions.Right);
            var quality = NewDropdown("QualityDrop", panel, new Vector2(x_widget, y), new Vector2(420, 52));
            y -= dy;
            NewText("L2", panel, new Vector2(x_label, y), new Vector2(300, 50), "settings.fps", 24f, UiColors.Text, true, TextAlignmentOptions.Right);
            var fps = NewDropdown("FpsDrop", panel, new Vector2(x_widget, y), new Vector2(420, 52));
            y -= dy;

            Slider NewLabeledSlider(string key, float min, float max)
            {
                NewText("LT", panel, new Vector2(x_label, y), new Vector2(300, 50), key, 24f, UiColors.Text, true, TextAlignmentOptions.Right);
                var sl = NewSlider("Sl", panel, new Vector2(x_widget, y), new Vector2(430, 26), min, max, (min + max) / 2f);
                y -= dy;
                return sl;
            }

            var master = NewLabeledSlider("settings.volume_master", 0f, 1f);
            var music = NewLabeledSlider("settings.volume_music", 0f, 1f);
            var sfx = NewLabeledSlider("settings.volume_sfx", 0f, 1f);
            var sens = NewLabeledSlider("settings.sensitivity", 0.1f, 3f);
            var cscale = NewLabeledSlider("settings.control_size", 0.7f, 1.4f);
            var copac = NewLabeledSlider("settings.control_opacity", 0.3f, 1f);

            // تگل‌ها دو ستونه
            var invert = NewToggle("T1", panel, new Vector2(-330f, y), "settings.invert_y", false);
            var shake = NewToggle("T2", panel, new Vector2(230f, y), "settings.camera_shake", true);
            y -= 66f;
            var haptics = NewToggle("T3", panel, new Vector2(-330f, y), "settings.haptics", true);
            var lefth = NewToggle("T4", panel, new Vector2(230f, y), "settings.left_handed", false);
            y -= 66f;
            var autoq = NewToggle("T5", panel, new Vector2(-330f, y), "settings.auto_quality", false);
            y -= 78f;

            var resetBtn = NewButton("Reset", panel, new Vector2(-140f, y), new Vector2(380, 66), "settings.reset_save", new Color(0.55f, 0.2f, 0.2f));
            var backBtn = NewButton("Back", panel, new Vector2(330f, y), new Vector2(220, 66), "common.back");

            // تأیید بازنشانی
            var confirmRoot = NewRect("ConfirmReset", panel, Vector2.zero, new Vector2(640, 260));
            NewImage(confirmRoot, new Color(0.16f, 0.1f, 0.1f, 0.98f));
            NewText("CT", confirmRoot, new Vector2(0, 70f), new Vector2(600, 90), "settings.reset_confirm", 26f, UiColors.Text);
            var yesBtn = NewButton("Yes", confirmRoot, new Vector2(-150f, -50f), new Vector2(240, 64), "common.yes", new Color(0.6f, 0.15f, 0.15f));
            var noBtn = NewButton("No", confirmRoot, new Vector2(150f, -50f), new Vector2(240, 64), "common.no");
            confirmRoot.gameObject.SetActive(false);

            SetField(comp, "qualityDropdown", quality);
            SetField(comp, "fpsDropdown", fps);
            SetField(comp, "masterVolumeSlider", master);
            SetField(comp, "musicVolumeSlider", music);
            SetField(comp, "sfxVolumeSlider", sfx);
            SetField(comp, "sensitivitySlider", sens);
            SetField(comp, "invertYToggle", invert);
            SetField(comp, "cameraShakeToggle", shake);
            SetField(comp, "hapticsToggle", haptics);
            SetField(comp, "leftHandedToggle", lefth);
            SetField(comp, "autoQualityToggle", autoq);
            SetField(comp, "controlScaleSlider", cscale);
            SetField(comp, "controlOpacitySlider", copac);
            SetField(comp, "resetSaveButton", resetBtn);
            SetField(comp, "confirmResetRoot", confirmRoot.gameObject);
            SetField(comp, "confirmYesButton", yesBtn);
            SetField(comp, "confirmNoButton", noBtn);
            SetField(comp, "backButton", backBtn);

            if (startHidden) root.gameObject.SetActive(false);
            return comp;
        }

        // ---------- آیتم انتخاب مرحله ----------

        public static void BuildLevelSelectItemPrefab()
        {
            var rt = NewRect("LevelSelectItem", null, Vector2.zero, new Vector2(640, 88));
            var img = NewImage(rt, UiColors.Btn);
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors; colors.highlightedColor = UiColors.BtnHigh; btn.colors = colors;
            NewText("Label", rt, Vector2.zero, new Vector2(620, 88), "-", 28f, UiColors.Text, localize: false);
            SavePrefab(rt.gameObject, PrefabsRoot + "/UI/LevelSelectItem.prefab");
        }

        // ---------- صحنه‌ها ----------

        public static void BuildAllScenes()
        {
            BuildBootstrapScene();
            BuildMainMenuScene();
            BuildLevelScene(LevelBlueprint.Level1());
            BuildLevelScene(LevelBlueprint.Level2());
            BuildLevelScene(LevelBlueprint.Level3());
            EditorSceneManager.OpenScene(GenRoot + "/Scenes/Bootstrap/Bootstrap.unity");
            Debug.Log("[FogWalker] همه صحنه‌ها ساخته شدند.");
        }

        private static void BuildBootstrapScene()
        {
            var scene = NewEmptyScene();
            var go = new GameObject("GameBootstrapper");
            var boot = go.AddComponent<GameBootstrapper>();
            var systems = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/UI/Systems.prefab");
            SetField(boot, "systemsPrefab", systems);
            SaveScene(scene, GenRoot + "/Scenes/Bootstrap/Bootstrap.unity");
        }

        private static void BuildMainMenuScene()
        {
            var scene = NewEmptyScene();

            // EventSystem
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = NewRect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<FogWalker.UI.Common.SafeAreaFitter>();

            // پس‌زمینه غروب تهران خیالی (گرادیان ساده)
            var bg = NewRect("Background", safe, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
            NewImage(bg, new Color(0.15f, 0.12f, 0.16f, 1f));

            // پنل اصلی
            var mainRoot = NewRect("MainPanel", safe, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            mainRoot.offsetMin = Vector2.zero; mainRoot.offsetMax = Vector2.zero;

            NewText("Title", mainRoot, new Vector2(0, 330f), new Vector2(900, 90), "game.title", 64f, UiColors.Accent);

            var continueBtn = NewButton("Continue", mainRoot, new Vector2(0, 150f), new Vector2(460, 86), "menu.continue");
            var newGameBtn = NewButton("NewGame", mainRoot, new Vector2(0, 45f), new Vector2(460, 86), "menu.new_game");
            var levelSelBtn = NewButton("LevelSelect", mainRoot, new Vector2(0, -60f), new Vector2(460, 86), "menu.level_select");
            var settingsBtn = NewButton("Settings", mainRoot, new Vector2(0, -165f), new Vector2(460, 86), "menu.settings");
            var quitBtn = NewButton("Quit", mainRoot, new Vector2(0, -270f), new Vector2(460, 86), "menu.quit");

            // پنل سختی
            var difficultyRoot = NewDialogPanel("DifficultyPanel", safe, new Vector2(720, 560));
            RectTransform diffPanel = (RectTransform)difficultyRoot.Find("DifficultyPanel_Panel");
            NewText("Title", diffPanel, new Vector2(0, 230f), new Vector2(660, 60), "menu.difficulty_title", 32f, UiColors.Text);
            var easyBtn = NewButton("Easy", diffPanel, new Vector2(0, 110f), new Vector2(460, 74), "difficulty.easy");
            var normalBtn = NewButton("Normal", diffPanel, new Vector2(0, 10f), new Vector2(460, 74), "difficulty.normal");
            var hardBtn = NewButton("Hard", diffPanel, new Vector2(0, -90f), new Vector2(460, 74), "difficulty.hard");
            var diffBack = NewButton("Back", diffPanel, new Vector2(0, -190f), new Vector2(460, 74), "common.back");
            difficultyRoot.gameObject.SetActive(false);

            // پنل انتخاب مرحله
            var lsRoot = NewDialogPanel("LevelSelectPanel", safe, new Vector2(800, 620));
            RectTransform lsPanel = (RectTransform)lsRoot.Find("LevelSelectPanel_Panel");
            NewText("Title", lsPanel, new Vector2(0, 260f), new Vector2(700, 56), "level.select_title", 32f, UiColors.Text);
            var content = NewRect("Content", lsPanel, new Vector2(0, -30f), new Vector2(680, 420));
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f; vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false; vlg.childControlWidth = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lsBack = NewButton("Back", lsPanel, new Vector2(0, -260f), new Vector2(320, 64), "common.back");
            var levelPanelComp = lsRoot.gameObject.AddComponent<LevelSelectPanel>();
            SetField(levelPanelComp, "contentParent", content);
            SetField(levelPanelComp, "itemPrefab", AssetDatabase.LoadAssetAtPath<Button>(PrefabsRoot + "/UI/LevelSelectItem.prefab"));
            SetField(levelPanelComp, "backButton", lsBack);
            lsRoot.gameObject.SetActive(false);

            // پنل تنظیمات
            var settingsPanelComp = BuildSettingsPanelContent(safe, startHidden: true);

            // MainMenuController
            var mmComp = mainRoot.gameObject.AddComponent<MainMenuController>();
            SetField(mmComp, "mainPanel", mainRoot.gameObject);
            SetField(mmComp, "difficultyPanel", difficultyRoot.gameObject);
            SetField(mmComp, "continueButton", continueBtn);
            SetField(mmComp, "newGameButton", newGameBtn);
            SetField(mmComp, "levelSelectButton", levelSelBtn);
            SetField(mmComp, "settingsButton", settingsBtn);
            SetField(mmComp, "quitButton", quitBtn);
            SetField(mmComp, "easyButton", easyBtn);
            SetField(mmComp, "normalButton", normalBtn);
            SetField(mmComp, "hardButton", hardBtn);
            SetField(mmComp, "difficultyBackButton", diffBack);
            SetField(mmComp, "levelSelectPanel", levelPanelComp);
            SetField(mmComp, "settingsPanel", settingsPanelComp);

            SaveScene(scene, GenRoot + "/Scenes/MainMenu/MainMenu.unity");
        }

        public static void AddScenesToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(GenRoot + "/Scenes/Bootstrap/Bootstrap.unity", true),
                new EditorBuildSettingsScene(GenRoot + "/Scenes/MainMenu/MainMenu.unity", true),
                new EditorBuildSettingsScene(GenRoot + "/Scenes/Levels/Level1_Boulevard.unity", true),
                new EditorBuildSettingsScene(GenRoot + "/Scenes/Levels/Level2_Bazaar.unity", true),
                new EditorBuildSettingsScene(GenRoot + "/Scenes/Levels/Level3_Bridge.unity", true),
            };
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[FogWalker] صحنه‌ها به Build Settings اضافه شدند.");
        }

        // ---------- کمک‌کارهای صحنه ----------

        private static Scene NewEmptyScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void SaveScene(Scene scene, string path)
        {
            EnsureDir(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
        }


    }
}
