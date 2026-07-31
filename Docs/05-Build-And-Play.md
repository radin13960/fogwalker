# ۰۵ — اجرا، ساخت خودکار و خروجی APK/AAB

## راه سریع (توصیه‌شده) — ۶ قدم

1. پروژه Unity 6 (قالب URP) بسازید و محتوای `FogWalkerTPS` را داخل آن کپی کنید (بجز پوشه `Build` و `tools` که اختیاری‌اند).
2. Unity را باز کنید تا پکیج‌ها Resolve شوند (URP, Input System, TMP, AI Navigation, Addressables, Test Framework).
3. **اختیاری ولی مهم برای فارسی:** یک فونت TTF فارسی (مثل Vazirmatn) در `Assets/_Project/Art/UI/Fonts/` بگذارید.
4. منوی **FogWalker → Setup → 0 - 🚀 ساخت کامل پروژه (همه مراحل یک‌جا)** را اجرا کنید.
   این منو خودش انجام می‌دهد: پوشه‌ها → ScriptableObjectها → تنظیمات Player و Layerها → فونت TMP → پری‌فب‌ها → صحنه Bootstrap/MainMenu/Level1‑3 → Build Settings.
5. در Project روی `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` دوبار کلیک و ▶️ Play.
6. در Editor با WASD/موس تست کنید (قتل Fire = کلیک چپ، Aim = کلیک راست، R = Reload، Q = Cover، Q مجدد یا عقب = خروج از کاور، G = نارنجک، E = تعامل، Shift = دویدن، C = خمیدن، Space = پرش، Esc = توقف).

## خروجی APK/AAB

### روش ۱ — منوی Unity
- **FogWalker → Build → APK توسعه (Development)** → `Build/Android/FogWalker.apk`
- **FogWalker → Build → AAB انتشار (Release)** → `Build/Android/FogWalker.aab`
(برای انتشار واقعی، Keystore خود را در Player Settings تنظیم کنید.)

### روش ۲ — خط فرمان (CI)
```
# لینوکس/مک
UNITY_EXE=/opt/unity/Editor/Unity ./tools/build_android.sh apk
./tools/build_android.sh aab

# ویندوز
set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.??f1\Editor\Unity.exe
tools\build_android.bat apk
```
مشخصات: IL2CPP / ARM64 / Min API 29 / Vulkan + GLES3 / Landscape قفل / بدون دسترسی اینترنت.

## رمزگشایی مشکلات رایج

| علامت | علت محتمل | راه‌حل |
|---|---|---|
| خطای `URP Asset یافت نشد` | پروژه URP نیست/Asset ثبت نشده | باید قالب URP باشد؛ `Project Settings > Graphics` را چک کنید |
| حروف فارسی جدا/ناقص | فونت فارسی نصب نیست | قدم ۳ را انجام دهید و Setup را دوباره بزنید |
| دکمه‌های لمسی کار نمی‌کنند | EventSystem ماژول قدیمی دارد | ماژول باید `InputSystemUIInputModule` باشد (ساخته Setup) |
| صفحه Loading می‌ماند | صحنه در Build Settings نیست | `FogWalker > Setup > 5` را دوباره اجرا کنید |
| خطای Package AI Navigation | پکیج نصب نشد | `Packages/manifest.json` مخزن را جایگزین کنید |
| دشمن‌ها حرکت نمی‌کنند | NavMesh ساخته نشده | در صحنه مرحله آبجکت `NavMesh/NavMeshSurface > Bake` (Setup خودش ساخته) |

## نکته صداقت درباره «فایل نصبی»
فایل APK کامپایل‌شده فقط با **Unity Editor روی دستگاه شما** قابل ساخت است — این مخزن همه کد، Asset و ابزار لازم را یک‌جا دارد تا با یک کلیک Setup و یک کلیک Build به APK برسید (معمولاً ۵ تا ۱۵ دقیقه اولین بار).
