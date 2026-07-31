# ۰۳ — ساخت صحنه‌ها و سیم‌کشی Inspector (فاز ۱)

> صحنه‌های `.unity` و `.asset` باینری/YAML پیچیده‌اند؛ به‌جای تولید دستی آن‌ها، این سند ساختار دقیق هر GameObject، Component و فیلد را می‌دهد. ScriptableObjectها با منوی `FogWalker > Setup > 2` خودکار ساخته می‌شوند.

## پیش‌نیاز

1. `FogWalker > Setup > 1 - ساخت ساختار پوشه‌ها`
2. `FogWalker > Setup > 2 - ساخت ScriptableObjectهای پایه` → در `Assets/_Project/ScriptableObjects/` این Assetها ساخته می‌شود:
   - `Quality/QP_Performance, QP_Balanced, QP_High`
   - `Difficulty/DS_Easy, DS_Normal, DS_Hard`
   - `Scenes/SceneCatalog_Main`
   - `Localization/LocTable_FA`
3. یک فونت فارسی OFL (مثل Vazirmatn) در `Art/UI/Fonts` و از آن **TMP Font Asset** بسازید (`Window > TextMeshPro > Font Asset Creator`) و در Project Settings > TMP به‌عنوان Default قرار دهید.
4. URP Asset طبق سند ۰۲ ساخته و در Graphics/Quality ثبت شود.

---

## صحنه ۱: `Bootstrap.unity` (در `Scenes/Bootstrap`)

```
Bootstrap (Scene)
└── GameBootstrapper          [Component: GameBootstrapper]
                                   • systemsPrefab ← پری‌فب "Systems" (پایین)
    (EventSystem لازم نیست — در MainMenu است)
```

صحنه را Save کنید و در **Build Settings اسلات ۰** قرار دهید.

## پری‌فب `Systems` (در `Prefabs/UI/Systems.prefab`)

```
Systems                                   (Root — DontDestroyOnLoad)
├── InputManagerObj                       [InputManager]
│      • actionsAsset  ← Settings/Input/GameInput.inputactions
├── SettingsManagerObj                    [SettingsManager]
│      • audioMixer   ← Art/Audio/MainMixer.mixer (ساخت دستی، پایین)
├── QualityManagerObj                     [QualityManager]
│      • performance ← QP_Performance, balanced ← QP_Balanced, high ← QP_High
├── LocalizationManagerObj                [LocalizationManager]
│      • tables       ← [LocTable_FA]
│      • useBuiltInRtlFix = ✓ (تا نصب RTLTMPro)
├── SceneLoaderObj                        [SceneLoader]
│      • catalog      ← SceneCatalog_Main
│      • minLoadingSeconds = 0.8
└── UIManagerObj                          [UIManager]
       • loadingScreen ← فرزند زیر
    └── LoadingScreen                   [LoadingScreen]
           • group           ← CanvasGroup روی همین آبجکت
           • progressBar     ← Slider فرزند
           • percentText     ← TMP_Text فرزند
        ├── Canvas          (Screen Space - Overlay, SortOrder=999, ScaleWithScreenSize 1920×1080)
        ├── CanvasGroup     (Alpha=0, BlocksRaycasts=off → LoadingScreen خودش کنترل می‌کند)
        ├── Background      (Image مشکی نیمه‌شفاف)
        ├── ProgressBar     (Slider)
        ├── PercentText     (TMP_Text + بدون LocalizedText؛ عدد است)
        └── HintText        (TMP_Text + LocalizedText: key=loading.hint)
```

**ساخت AudioMixer:** `Assets > Create > Audio Mixer`؛ گروه‌ها: `Master → Music / SFX / UI / Ambience`. روی هرکدام از Master/Music/SFX راست‌کلیک روی Volume و `Expose 'Volume' to script` و نام پارامتر را در پنجره AudioMixer (بخش Exposed Parameters) به **MasterVolume / MusicVolume / SFXVolume** تغییر دهید.

---

## صحنه ۲: `MainMenu.unity` (در `Scenes/MainMenu`)

```
MainMenu (Scene)
├── Directional Light (خاموش/زاویه ملایم — فقط برای پیش‌نمایش)
├── Main Camera
├── EventSystem            [EventSystem + **InputSystemUIInputModule**]
└── Canvas                 (Screen Space - Overlay, ScaleWithScreenSize 1920×1080, Match=0.5)
    └── SafeArea           [RectTransform Full + SafeAreaFitter]
        ├── MainPanel
        │     ├── Title           TMP_Text + LocalizedText(game.title)
        │     ├── ContinueButton  Button → فرزند TMP + LocalizedText(menu.continue)
        │     ├── NewGameButton   Button → LocalizedText(menu.new_game)
        │     ├── LevelSelectBtn  Button → LocalizedText(menu.level_select)
        │     ├── SettingsButton  Button → LocalizedText(menu.settings)
        │     ├── QuitButton      Button → LocalizedText(menu.quit)
        │     └── [Component: MainMenuController روی ریشه MainPanel یا SafeArea]
        │            newGame/continue/levelSelect/settings/quit ← دکمه‌های بالا
        │            mainPanel, difficultyPanel, levelSelectPanel, settingsPanel ← مراجع
        ├── DifficultyPanel       (پیش‌فرض: غیرفعال)
        │     ├── Title           LocalizedText(menu.difficulty_title)
        │     ├── EasyButton      → LocalizedText(difficulty.easy)
        │     ├── NormalButton    → LocalizedText(difficulty.normal)
        │     ├── HardButton      → LocalizedText(difficulty.hard)
        │     └── BackButton      → LocalizedText(common.back)
        ├── LevelSelectPanel      (پیش‌فرض: غیرفعال) [Component: LevelSelectPanel]
        │     • contentParent ← Content (VerticalLayoutGroup + ContentSizeFitter)
        │     • itemPrefab    ← پری‌فب Button ساده با TMP_Text فرزند (Prefabs/UI)
        │     • backButton    ← دکمه بازگشت
        └── SettingsPanel         (پیش‌فرض: غیرفعال) [Component: SettingsPanel]
              • qualityDropdown  (TMP_Dropdown — گزینه‌ها با کد فارسی پر می‌شوند)
              • fpsDropdown      (TMP_Dropdown — گزینه‌های 30/45/60)
              • master/music/sfx Sliders (0..1)
              • sensitivitySlider (0.1..3)
              • invertY, cameraShake, haptics, leftHanded Toggles
              • controlScaleSlider (0.7..1.4), controlOpacitySlider (0.3..1)
              • resetSaveButton + confirmResetRoot(غیرفعال) + confirmYes/confirmNo
              • برچسب همه ویجت‌ها: TMP_Text + LocalizedText با keyهای settings.*
```

### فهرست keyهای متنی (LocTable_FA به‌صورت خودکار می‌سازدشان)

`game.title, menu.continue, menu.new_game, menu.level_select, menu.settings, menu.quit, menu.difficulty_title, difficulty.easy, difficulty.normal, difficulty.hard, common.back, common.yes, common.no, common.locked, loading.hint, settings.title, settings.quality, settings.fps, settings.volume_master, settings.volume_music, settings.volume_sfx, settings.sensitivity, settings.invert_y, settings.camera_shake, settings.haptics, settings.left_handed, settings.control_size, settings.control_opacity, settings.reset_save, settings.reset_confirm, level.select_title`

## Build Settings

| اسلات | صحنه |
|---|---|
| 0 | `Scenes/Bootstrap/Bootstrap.unity` |
| 1 | `Scenes/MainMenu/MainMenu.unity` |

## اجرای تست فاز ۱

همیشه از صحنه **Bootstrap** پروژه را اجرا کنید (نه MainMenu) تا سرویس‌ها ساخته شوند. اگر مستقیم MainMenu را Play کنید، `MainMenuController` در لاگ خطای راهنما می‌دهد.
