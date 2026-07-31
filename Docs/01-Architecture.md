# ۰۱ — معماری پروژه FogWalker

## ۱. نمای کلی وابستگی‌ها

```
                 ┌──────────────────────────┐
                 │   GameBootstrapper       │  (صحنه Bootstrap — نقطه ورود واحد)
                 └────────────┬─────────────┘
                              │ ساخت و ثبت سرویس‌ها در ServiceLocator
        ┌──────────┬──────────┼───────────┬────────────┬─────────────┐
        ▼          ▼          ▼           ▼            ▼             ▼
   SaveSystem  InputManager GameState  Quality    Localization   SceneLoader
   (POCO)      (MB)         Manager    Manager +  Manager (MB)   (MB)
                              (POCO)   SettingsManager (MB)         │
                                       Localization/Audio           ▼
                                                              MainMenu / HUD / Levels
```

قوانین وابستگی:
- سرویس‌ها فقط از طریق `ServiceLocator.Get<T>()` به هم دسترسی دارند؛ مرجع SerializeField بین صحنه‌ها ممنوع.
- کد gameplay (فاز ۲+) *نباید* به MainMenu ارجاع دهد.
- سیستم‌های داده (Save/State) کلاس‌های خالص C# (POCO) هستند تا در EditMode قابل تست باشند.
- هیچ Singleton دست‌سازی با `static Instance` ممنوع؛ تک‌نسخه‌بودن با Bootstrapper و `DontDestroyOnLoad` تضمین می‌شود.

## ۲. ماژول‌ها و مسئولیت‌ها (فاز ۱)

| ماژول | نوع | مسئولیت |
|---|---|---|
| `GameBootstrapper` | MonoBehaviour | نقطه ورود، ساخت سیستم‌ها، رفتن به MainMenu |
| `GameStateManager` | POCO | ماشین‌حالت بازی با Transitionهای معتبر، timeScale، دروازه ورودی |
| `ServiceLocator` | static | ثبت/بازیابی سرویس‌ها |
| `SceneLoader` | MonoBehaviour | بارگذاری async صحنه‌ها + صفحه Loading + حداقل زمان نمایش |
| `SceneCatalog` | SO | نام صحنه‌ها و فهرست مراحل (بدون Hardcode) |
| `SaveSystem` + `SaveData` | POCO | ذخیره JSON + نسخه‌بندی + Checksum + Backup |
| `SettingsManager` | MonoBehaviour | اعمال و پایش تنظیمات (کیفیت، FPS، صدا، کنترل، زبان) |
| `QualityProfileSO` / `QualityManager` | SO+MB | ۳ پروفایل کیفیت روی URP/QualitySettings |
| `InputManager` | MonoBehaviour | نگاشت‌ها (Gameplay/UI)، روشن/خاموش‌کردن ورودی، رویداد Pause |
| `LocalizationManager/Table/LocalizedText` | MB+SO | فارسی/انگلیسی، RTL fix داخلی |
| `PersianTextUtility` | static | Reshape فارسی/عربی + ارقام فارسی |
| `UIManager` / `LoadingScreen` | MonoBehaviour | صفحه بارگذاری (HUD در فاز ۲ اضافه می‌شود) |
| `MainMenuController` و پنل‌ها | MonoBehaviour | منوی اصلی، سختی، انتخاب مرحله، تنظیمات |

## ۳. ماشین‌حالت بازی

| State | timeScale | ورودی gameplay | Transitionهای مجاز به |
|---|---|---|---|
| Bootstrap | 1 | خاموش | MainMenu, Loading |
| MainMenu | 1 | خاموش | Loading |
| Loading | 1 | خاموش | Playing, MainMenu |
| Playing | 1 | روشن | Paused, Cutscene, PlayerDead, LevelComplete |
| Paused | **0** | خاموش | Playing, Loading (خروج به منو) |
| Cutscene | 1 | خاموش | Playing |
| PlayerDead | 1 | خاموش | Loading, MainMenu |
| LevelComplete | 1 | خاموش | Loading, MainMenu |

Transition غیرمجاز → `false` + هشدار در لاگ توسعه (Debug فقط در Editor/Development).

## ۴. قراردادها (بازبینی کد)

- Namespace ریشه: `FogWalker.*` — یکپارچه با ساختار پوشه.
- همه فیلدهای قابل‌تنظیم با `[SerializeField, Tooltip]`.
- ممنوع در `Update`: `FindObjectOfType`، `GetComponent` مکرر، `string.Format`/تخصیص رشته، LINQ.
- `Debug.Log` فقط از طریق `GameLog` (در بیلد نهایی خاموش؛ Error همیشه‌روشن).
- متن فارسی فقط از `LocalizationManager.GetText(key)`.
- رویداد بین‌سیستمی: Event خالص C# یا `VoidEventChannelSO` (کانال‌های بیشتر در فاز ۲).

## ۵. درخت کامل پوشه‌ها (با `Setup > 1` ساخته می‌شود)

```
Assets/
  _Project/
    Art/{Materials,Models,Textures,Animations,VFX,Audio,UI}
    Scenes/{Bootstrap,MainMenu,Levels,Shared}
    Prefabs/{Player,Enemies,Weapons,Environment,UI,VFX}
    Scripts/{Core,Gameplay/{Player,Weapons,AI,Missions,Combat,Interactions},UI,Audio,Save,Optimization,Utilities}
    ScriptableObjects/{Weapons,Enemies,Missions,Difficulty,Audio,Localization,Quality,Scenes}
    Settings/Input
    Addressables/
    Tests/{EditMode,PlayMode}
  ThirdParty/
```

## ۶. Assembly Definitionها

| asmdef | مراجع | نکته |
|---|---|---|
| `FogWalker.Runtime` | InputSystem, TextMeshPro, URP.Runtime | همه اسکریپت‌های Runtime |
| `FogWalker.Tests.EditMode` | Runtime, TestRunner | فقط Editor |

> در فاز ۲ در صورت بزرگ‌شدن، `FogWalker.Gameplay` جدا می‌شود; فعلاً یکدست‌نگه‌داشتن ساده‌تر است.
