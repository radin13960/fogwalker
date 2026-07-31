# ۰۴ — چک‌لیست تست فاز ۱

## الف) تست‌های خودکار (EditMode)

اجرای: `Window > Test Runner > EditMode > Run All`

| تست | کلاس | چه چیزی را اثبات می‌کند |
|---|---|---|
| Roundtrip | `SaveSystemTests` | ذخیره و خواندن SaveData بدون اتلاف داده |
| CorruptMain_RecoversFromBackup | `SaveSystemTests` | خرابی فایل اصلی → بازیابی از `.bak` |
| BothCorrupt_DefaultsAndQuarantine | `SaveSystemTests` | خرابی هر دو فایل → Save پیش‌فرض + قرنطینه فایل خراب بدون کرش |
| ChecksumTampering_Rejected | `SaveSystemTests` | دست‌کاری payload شناسایی شود |
| Defaults_AreSane | `SettingsDataTests` | پیش‌فرض‌های تنظیمات معتبر (کیفیت، FPS، حجم صدا) |
| Difficulty_IsPersistedInProgress | `SettingsDataTests` | درجه سختی در Progress ذخیره می‌شود |
| InitialState_IsBootstrap | `GameStateManagerTests` | حالت اولیه |
| LegalFlow_TransitionsWork | `GameStateManagerTests` | مسیر Bootstrap→Menu→Loading→Playing→Pause→Resume |
| IllegalTransition_IsBlocked | `GameStateManagerTests` | Transition غیرمعتبر رد شود و حالت عوض نشود |
| Pause_SetsTimeScaleZero | `GameStateManagerTests` | `timeScale=0` در Pause و بازیابی بعد خروج |
| InputGate_TogglesOnPlaying | `GameStateManagerTests` | دروازه ورودی فقط در Playing روشن |

## ب) تست دستی (روی Editor + دستگاه)

### منو و ذخیره
- [ ] اجرا از Bootstrap → صفحه Loading → MainMenu بدون خطای کنسول.
- [ ] «شروع بازی جدید» → پنل سختی → انتخاب هر سه سطح → بارگذاری صحنه مرحله (در فاز ۱: لاگ/رفتار SceneLoader بررسی شود چون صحنه مرحله هنوز نیست → باید خطای کنترل‌شده بدهد نه کرش).
- [ ] «ادامه بازی» فقط وقتی فعال است که Save وجود دارد.
- [ ] تغییر یک تنظیم → خروج کامل از بازی → اجرای مجدد → مقدار حفظ شده.
- [ ] خراب‌کردن دستی فایل `persistentDataPath/save.json` (نوشتن متن تصادفی) → اجرای بازی → بازی کرش نکند و Save پیش‌فرض ساخته شود.
- [ ] «بازنشانی پیشرفت» → پنجره تأیید → بله → دکمه ادامه غیرفعال شود.

### تنظیمات
- [ ] تغییر کیفیت Performance/Balanced/High → بررسی مقدار Render Scale در Frame Debugger/GameView.
- [ ] FPS هدف 30/45/60 → `Application.targetFrameRate` مطابق شود.
- [ ] اسلایدر صداها → تغییر شنوایی در میکسر (در Editor از Audio Mixer پنجره).
- [ ] همه Toggleها (معکوس Y، لرزش دوربین، لرزش لمسی، چپ‌دست) ذخیره شوند.

### ورودی و حالت‌ها
- [ ] با فعال‌بودن نقشه UI، نقشه Gameplay در منو **غیرفعال** باشد (Window > Analysis > Input Debugger).
- [ ] دکمه Escape در Play mode → رویداد `OnPauseRequested` (لاگ توسعه در فاز ۲ متصل می‌شود).
- [ ] چرخش دستگاه: بازی در Landscape قفل بماند (Portrait رخ ندهد).
- [ ] حالت Airplane Mode: اجرای کامل بدون خطا.
- [ ] Safe Area: روی دستگاه با ناچ، دکمه‌ها داخل ناحیه امن باشند.

### RTL و متن
- [ ] همه برچسب‌های منو فارسی، راست‌چین و بدون حروف جداازهم.
- [ ] درصد Loading با ارقام فارسی نمایش داده شود.

### کیفیت کد
- [ ] کنسول بدون خطای Compile / NullReference / Missing Reference.
- [ ] هیچ دکمه‌ای بدون Listener نباشد.
- [ ] `GameLog` در Development Build پیام Info چاپ کند و در Release نکند.

## ج) معیار خروج از فاز ۱ (Definition of Done)
- همه تست‌های EditMode سبز. ✅
- MainMenu کاملاً کاربردی از روی Save واقعی. ✅
- هیچ وابستگی اینترنتی در کد نیست (`Internet Access: Not Required`). ✅
- آماده شروع فاز ۲ (PlayerController + دوربین + اسلحه پایه). ✅
