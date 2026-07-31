# ۰۲ — تنظیمات Unity، URP و Android

## ۱. نسخه‌ها و پکیج‌ها

- **Unity 6 LTS** — سری `6000.0.x`.
- پکیج‌ها (در `Packages/manifest.json` آمده؛ یونیتی خودش نسخه سازگار را حل می‌کند):

| پکیج | دلیل |
|---|---|
| Universal RP 17 | رندر موبایل بهینه، SRP Batcher، GPU Instancing |
| Input System 1.11 | ورودی لمسی/گیم‌پد/کیبورد یکپارچه |
| Cinemachine 3.1 | دوربین سوم‌شخص فاز ۲ (Collision، Aim) |
| AI Navigation 2.0 | NavMesh دشمنان (فاز ۲+) |
| Addressables 2.2 | مدیریت صحنه‌های سنگین (فاز ۳+) |
| Test Framework 1.4 | تست‌های EditMode/PlayMode |
| uGUI 2.0 | شامل TextMeshPro |

## ۲. تنظیمات ProjectSettings

### Player
| گزینه | مقدار |
|---|---|
| Company / Product | "Mehnord Studio" / "FogWalker" *(تخیلی)* |
| Color Space | **Linear** |
| Scripting Backend | **IL2CPP** |
| API Compatibility | .NET Standard 2.1 |
| Target Architectures | **ARM64** (ARMv7 فقط در صورت نیاز بازار) |
| Min API Level | **Android 10 (API 29)** |
| Target API Level | Highest Installed (الزام Google Play در انتشار) |
| Graphics APIs | **Vulkan, OpenGLES3** (فالبک پس از تست) |
| Active Input Handling | **Input System (New)** |
| Orientation | فقط **Landscape Left + Landscape Right**؛ Portrait و Auto-Rotation خاموش |
| Internet Access | **Not Required** (تضمین آفلاین‌بودن) |
| Write Permission / Install Location | Internal / Auto |
| Managed Stripping Level | Low (فاز ۱) → Medium پس از پایداری |
| Incremental GC | روشن |

### Quality
> ⚠️ **فقط یک سطح Quality** در `Edit > Project Settings > Quality` نگه دارید (نام پیشنهادی: `Runtime`) و پیش‌فرض اندروید همان باشد. سه پروفایل ما از مسیر `QualityManager` + `QualityProfileSO` اعمال می‌شوند، نه سطح‌های Quality یونیتی.

### Physics — Layerها (در `Tags and Layers` بسازید)
`6:Player  7:Enemy  8:Environment  9:Hitbox  10:Interactable  11:Cover`
ماتریس برخورد دقیق و Tagها در فاز ۲ سند می‌شود.

## ۳. URP Asset و Renderer

از `Assets > Create > Rendering > URP Asset (with Universal Renderer)` یک Asset با نام `URP_FogWalker` بسازید و در `Project Settings > Graphics` و `Quality` ثبتش کنید. تنظیمات پایه (منبع حقیقت = مقادیر اولیه؛ تغییرات زمان‌اجرا با پروفایل‌ها):

| گزینه | مقدار پایه |
|---|---|
| HDR | روشن |
| MSAA | 2x |
| Render Scale | 1.0 |
| Depth Texture | **روشن** (برای کمرا اکشن‌ها/افکت‌های بعدی) |
| Opaque Texture | خاموش (صرفه‌جویی) |
| Main Light | Per Pixel، Cast Shadows روشن، Shadow Resolution 2048، Distance 60، Cascades 2 |
| Additional Lights | Per Pixel، حداکثر 4 |
| Soft Shadows | روشن (در Performance از مسیر پروفایل کاهش می‌یابد) |
| Renderer: Rendering Path | Forward، Depth Priming خاموش |
| SRP Batcher / Dynamic Batching | روشن |

**نکته مهم:** `QualityManager` در زمان اجرا یک **کپی Runtime** از URP Asset می‌سازد و مقادیر را روی کپی اعمال می‌کند تا Asset اصلی در Editor تغییر نکند.

### سه پروفایل کیفیت (مقادیر نمونه اولیه — با Setup ساخته می‌شود)

| پارامتر | Performance (0) | Balanced (1) | High (2) |
|---|---|---|---|
| Render Scale | 0.75 | 0.85 | 1.0 |
| MSAA | Off | 2x | 4x |
| HDR | روشن | روشن | روشن |
| Main Light Shadow Res | 1024 | 2048 | 2048 |
| Shadow Distance | 35 | 60 | 90 |
| Shadow Cascades | 1 | 2 | 3 |
| Pixel Light Count | 2 | 3 | 4 |
| LOD Bias | 0.7 | 1.0 | 1.5 |
| Texture Mipmap Limit | 1 | 0 | 0 |
| Post Processing | خاموش | Bloom+Vignette سبک | کامل (با احتیاط) |
| Particle Budget | 60 | 100 | 200 |
| Draw Distance (Far Clip) | 100 | 150 | 220 |
| هدف FPS | 45–60 میان‌رده | 45–60 پایدار | 60 پرچمدار |

تنظیم **FPS هدف** مستقل از پروفایل است و از منوی تنظیمات (30/45/60) کنترل می‌شود.

## ۴. صدا (Audio Mixer)

طبق `Docs/03` یک Mixer با گروه‌های `Master → Music / SFX / UI / Ambience` بسازید و سه پارامتر `MasterVolume`, `MusicVolume`, `SFXVolume` را Expose کنید (نام‌ها دقیقاً همین؛ `SettingsManager` به این نام‌ها متصل است).

## ۵. Build Android

- تست داخلی: `Build Settings > Android > Development Build` → **APK**.
- انتشار: تیک **Build App Bundle (Google Play)** → **AAB**، Keystore بسازید و در Player Settings ثبت کنید.
- پیش از ساخت نهایی: `GameLog` به‌صورت خودکار در بیلد Release Info/Warn را حذف می‌کند (Conditional Compilation).
