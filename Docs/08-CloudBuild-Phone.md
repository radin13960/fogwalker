# ~~ساخت فایل نصبی (APK) فقط با گوشی~~ — ⚠️ این سند منسوخ شد

> ⚠️ **جایگزین:** روش این سند (game-ci) به‌خاطر حذف پشتیبانی فعال‌سازی دستی لایسنس Personal توسط Unity دیگر کار نمی‌کند.
> روش جدید و ساده‌تر — ساخت با **سرویس ابری خود یونیتی (Unity Build Automation)** بدون هیچ دردسر لایسنس:
> 👉 **`Docs/09-UnityCloudBuild-Phone.md`**

| مورد | مقدار |
|------|--------|
| هزینه | **رایگان** (مخزن Public = دقیقه‌ی ساخت نامحدود) |
| کار دستی شما | حدود ۱۵–۲۰ دقیقه (فقط بار اول) |
| زمان ساخت APK | حدود ۲۰–۶۰ دقیقه صبر روی سرور |
| ابزار لازم | مرورگر کروم روی گوشی + برنامه‌ی «Files by Google» (یا هر مدیریت فایل) |

> 💡 در تمام مراحل، در کروم از منوی ⋮ گزینه‌ی **«Desktop site / نسخه‌ی کامپیوتر»** را روشن کنید؛ بعضی دکمه‌ها در حالت موبایل مخفی‌اند.

---

## مرحله‌ی ۱ — ساخت دو حساب رایگان (۵ دقیقه)

1. **حساب GitHub** ← سایت `github.com/signup` — فقط ایمیل + رمز، و تأیید ایمیل.
2. **حساب Unity** ← سایت `id.unity.com` ← *Create a Unity ID* — باز هم فقط ایمیل.

---

## مرحله‌ی ۲ — ساخت مخزن و آپلود فایل ZIP

1. در github.com دکمه‌ی **New repository** (یا از منوی +) را بزنید.
2. نام دلخواه مثلاً `fogwalker` — نوع: **Public** — بدون هیچ تیکی — **Create repository**.
3. در صفحه‌ی مخزن: **Add file → Upload files**.
4. فایل **`FogWalkerTPS_Full.zip`** را از پوشه‌ی Downloads انتخاب کنید و **Commit changes** را بزنید.
   (همین یک فایل کافی است؛ لازم نیست از حالت فشرده خارجش کنید.)

---

## مرحله‌ی ۳ — ساخت «فایل دستور ساخت» (فقط یک‌بار)

1. دوباره **Add file → Create new file**.
2. در کادر نام فایل دقیقاً این را تایپ کنید (اسلش‌ها مهم‌اند، خودش پوشه می‌سازد):

   ```
   .github/workflows/fogwalker-cloud-build.yml
   ```
3. کل متن زیر را کپی و در کادر بزرگ **الصاق** کنید:

```yaml
name: "FogWalker — ساخت ابری"

on:
  workflow_dispatch:
    inputs:
      step:
        description: "کدام مرحله اجرا شود؟"
        required: true
        default: "build"
        type: choice
        options:
          - build
          - activation

env:
  UNITY_VERSION: 6000.0.34f1

jobs:
  activation:
    if: inputs.step == 'activation'
    name: دریافت فایل فعال‌سازی (.alf)
    runs-on: ubuntu-latest
    steps:
      - name: Request manual activation file
        uses: game-ci/unity-request-activation-file@v2
        id: getManualLicenseFile
        with:
          unityVersion: ${{ env.UNITY_VERSION }}
      - name: Upload activation file
        uses: actions/upload-artifact@v4
        with:
          name: unity-activation-file
          path: ${{ steps.getManualLicenseFile.outputs.filePath }}

  build:
    if: inputs.step == 'build'
    name: ساخت FogWalker.apk
    runs-on: ubuntu-latest
    timeout-minutes: 120
    steps:
      - name: Free disk space
        uses: jlumbroso/free-disk-space@main
        with:
          tool-cache: true
          android: true
          dotnet: true
          haskell: true
          swap-storage: true
          docker-images: true
          large-packages: true
      - name: Checkout
        uses: actions/checkout@v4
      - name: Extract project zip
        run: |
          unzip -o FogWalkerTPS_Full.zip -d extracted
          ls extracted/FogWalkerTPS
      - name: Unity Build (game-ci)
        uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          unityVersion: ${{ env.UNITY_VERSION }}
          targetPlatform: Android
          projectPath: extracted/FogWalkerTPS
          buildMethod: FogWalker.EditorTools.BuildScript.BuildAndroidApkCI
      - name: Upload APK
        uses: actions/upload-artifact@v4
        with:
          name: FogWalker-APK
          path: extracted/FogWalkerTPS/Build/Android/FogWalker.apk
          if-no-files-found: error
```

4. پایین صفحه **Commit changes** را بزنید. ✅

---

## مرحله‌ی ۴ — گرفتن لایسنس یونیتی (فقط بار اول)

یونیتی برای ساخت خروجی به «لایسنس» نیاز دارد (رایگانِ Personal). یک‌بار این مسیر را می‌رویم:

1. در مخزن به تب **Actions** بروید. اگر کادری با دکمه‌ی **«I understand my workflows, go ahead and enable them»** دیدید، بزنید.
2. از ستون چپ روی **«FogWalker — ساخت ابری»** کلیک کنید.
3. سمت راست دکمه‌ی **Run workflow** → منوی کشویی **step را روی `activation`** بگذارید → **Run workflow** سبز.
4. ۲–۳ دقیقه صبر کنید تا تیک سبز بخورد. روی آن اجرا کلیک کنید → پایین صفحه بخش **Artifacts** → روی **unity-activation-file** بزنید (یک zip دانلود می‌شود).
5. با «Files by Google» آن zip را باز کنید؛ داخلش فایلی با پسوند **`.alf`** است.
6. در مرورگر به **`license.unity3d.com`** بروید ← با حساب Unity (مرحله‌ی ۱) وارد شوید.
7. گزینه‌ی **Manual Activation** ← **Upload** و فایل `.alf` را انتخاب کنید ← Next.
8. نوع لایسنس: **Unity Personal** ← اگر پرسید: **«I don't use Unity in a professional capacity»** ← **Download license file**.
9. فایلی با پسوند **`.ulf`** دانلود می‌شود. آن را نگه دارید (این کلید لایسنس شماست).

---

## مرحله‌ی ۵ — دادن لایسنس به سازنده (سکرت)

1. فایل `.ulf` را با «Files by Google» باز کنید ← **Open as → Text** (یا هر ویرایشگر متن).
2. **کل متن را از اول تا آخر** انتخاب (Select all) و **کپی** کنید. (متن بین `<root> ... </root>` است.)
3. برگردید به مخزن GitHub ← **Settings** ← منوی چپ: **Secrets and variables → Actions**.
4. **New repository secret** ← در Name دقیقاً بنویسید `UNITY_LICENSE` ← در کادر Secret متن را الصاق کنید ← **Add secret**.

---

## مرحله‌ی ۶ — ساخت APK 🎉

1. تب **Actions** ← **«FogWalker — ساخت ابری»** ← **Run workflow** ← این‌بار **step روی `build`** ← سبز.
2. **۲۰ تا ۶۰ دقیقه** صبر کنید (می‌توانید صفحه را ببندید؛ سرور کارش را می‌کند).
3. وقتی تیک سبز خورد: روی همان اجرا کلیک کنید ← پایین صفحه **Artifacts** ← **FogWalker-APK**.
4. یک zip دانلود می‌شود؛ با «Files» بازش کنید ← داخلش **FogWalker.apk** است ← روی آن بزنید.
5. اگر گوشی اجازه نداد: **تنظیمات ← امنیت ← نصب برنامه‌های ناشناس (Install unknown apps)** ← برای Chrome/Files روشنش کنید ← دوباره نصب.
6. بازی اجرا می‌شود! 🎮 (گرافیک فعلاً placeholder است — طبق طراحی.)

---

## دفعات بعد (خیلی ساده) 🔁

- برای گرفتن APK جدید فقط: **Actions ← Run workflow ← build**. تمام!
- اگر از من ZIP به‌روز گرفتید: فقط **Add file → Upload files** و ZIP جدید را جایگزین کنید (Commit)، بعد build.

---

## عیب‌یابی 🩹

| علامت | دلیل | راه‌حل |
|------|------|--------|
| اجرای build قرمز می‌شود با خطای *License activation failed* | سکرت اشتباه یا ناقص کپی شده | متن `.ulf` را **کامل** (از اولین تا آخرین خط) دوباره کپی و سکرت را ویرایش کنید |
| خطای *FogWalkerTPS_Full.zip: No such file* | ZIP در مخزن نیست | مرحله‌ی ۲ را دوباره انجام دهید؛ نام فایل دقیقاً همین باشد |
| خطای *manifest … not found* | نسخه‌ی Unity عوض شده | در فایل workflow مقدار `UNITY_VERSION` باید دقیقاً `6000.0.34f1` بماند (ایمیج اندروید نسخه‌های جدیدتر فعلاً در game-ci خراب است) |
| خطای *هیچ صحنه‌ای در Build Settings نیست* | فایل workflow قدیمی/ناقص است | متن مرحله‌ی ۳ همین سند را دوباره کامل جایگزین کنید (باید `BuildAndroidApkCI` داشته باشد) |
| دکمه‌ی Run workflow یا Artifacts دیده نمی‌شود | حالت موبایل مرورگر | در کروم ⋮ ← **Desktop site** |
| بیش از ۱ ساعت طول کشید | برای اولین ساخت IL2CPP طبیعی است | تا ۱۲۰ دقیقه صبر کنید |
| گوشی هنگام نصب هشدار می‌دهد | امضای دیباگ (طبیعی) | «نصب از منابع ناشناس» را فقط برای مرورگر/Files روشن کنید |

## نکته‌های مهم ⚠️

- نسخه‌ی Unity روی **6000.0.34f1** پین شده چون ایمیج اندرویدِ سالم‌ش در ساخت ابری موجود است (نسخه‌های جدیدتر در game-ci مشکل SDK دارند). بازی روی گوشی شما بی‌مشکل اجرا می‌شود؛ برای «انتشار رسمی در فروشگاه» بعداً با کامپیوتر به آخرین پچ ارتقا می‌دهیم.
- فایل `.ulf` را گم نکنید؛ کلید لایسنس شماست.
- Artifactها تا ۹۰ روز روی GitHub می‌مانند؛ APK را روی گوشی خودتان هم نگه دارید.

---

*هر مرحله گیر کردید: متن دقیق خطا یا اسم مرحله را برای من بفرستید تا همان‌جا راهنمایی‌تان کنم.* 🤝
