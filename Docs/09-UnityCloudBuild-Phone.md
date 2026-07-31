# ساخت APK فقط با گوشی — روش نهایی: ساخت ابری خود یونیتی ☁️📱

**ایده:** سرویس رسمی **Unity Build Automation** (در Unity Cloud) بازی را از روی مخزن GitHub شما می‌سازد. لایسنس را خودِ یونیتی مدیریت می‌کند؛ هیچ فایل `.ulf` یا اکشن جانبی لازم نیست.

| مورد | مقدار |
|------|--------|
| هزینه | **رایگان** — هر ماه ۱۰۰ دقیقه ساخت لینوکس رایگان (اندروید روی لینوکس ساخته می‌شود) |
| پیش‌نیاز | حساب Unity ID + مخزن GitHub که پروژه در ریشه‌اش باز شده باشد |
| خروجی | APK قابل دانلود از داشبورد یونیتی |

---

## مرحله‌ی ۰ — باز کردن پروژه در ریشه‌ی مخزن (یک‌بار)

در گیت‌هاب، فایل `.github/workflows/fogwalker-cloud-build.yml` را باز کنید (آیکون ✏️) و **کل محتوایش** را با متن همین کادر جایگزین کنید، سپس Commit:

```yaml
name: "FogWalker — آماده‌سازی پروژه"

on:
  workflow_dispatch: {}

permissions:
  contents: write

jobs:
  unpack:
    name: "باز کردن پروژه در ریشه‌ی مخزن"
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      - name: Unzip project to repo root
        run: |
          unzip -o FogWalkerTPS_Full.zip -d extracted
          shopt -s dotglob
          cp -r extracted/FogWalkerTPS/* .
          rm -rf extracted
      - name: Commit unpacked project
        run: |
          git config user.email "fogwalker-bot@users.noreply.github.com"
          git config user.name "FogWalker Bot"
          git add -A
          git commit -m "Unpack Unity project" || echo "no changes"
          git push
```

بعد: تب **Actions** ← **«FogWalker — آماده‌سازی پروژه»** ← **Run workflow** ← سبز. بعد از تیک سبز ✅، صفحه‌ی اصلی مخزن باید پوشه‌های **Assets** و **Packages** و **ProjectSettings** را نشان بدهد.

---

## مرحله‌ی ۱ — ورود به Unity Cloud و فعال‌سازی Build Automation

1. در کروم (حالت **Desktop site**) به `cloud.unity.com` بروید و با Unity ID وارد شوید.
2. از منوی سرویس‌ها **Build Automation** (در بخش DevOps) را باز کنید و اگر خواست «پروژه/سازمان» بسازید، یک نام دلخواه (مثلاً `fogwalker`) بدهید.
3. اتصال به گیت‌هاب: **Connect to GitHub** ← با حساب GitHub خود مجوز می‌دهید و مخزن `fogwalker` را انتخاب می‌کنید.

## مرحله‌ی ۲ — ساخت هدف (Target) اندروید

یک Build Target جدید با این تنظیمات بسازید:

| فیلد | مقدار |
|------|--------|
| Platform | **Android** |
| Branch | `main` |
| Project subfolder | *(خالی — ریشه‌ی مخزن)* |
| Unity version | Auto (از روی ProjectVersion.txt نسخه 6000.0.34f1 انتخاب می‌شود) |
| Machine | Linux (رایگان/Micro) |
| Advanced → **Pre-Export Method** | `FogWalker.EditorTools.SetupFactory.BuildEverything` |
| بقیه | پیش‌فرض |

> ⚙️ متد Pre-Export همان «ساخت کامل پروژه» است که روی سرور، صحنه‌ها و پری‌فب‌ها را می‌سازد.

## مرحله‌ی ۳ — ساخت و دانلود 🎮

1. روی Target دکمه‌ی **Build** را بزنید.
2. بین ۲۰ تا ۹۰ دقیقه صبر (اولین بار: ایمپورت کامل پروژه).
3. در تاریخچه‌ی ساخت‌ها (Build History)، روی ساخت موفق کلیک کنید ← **Download** ← فایل `.apk`.
4. نصب روی گوشی: اجازه‌ی «نصب از منابع ناشناس» → نصب. ✅

## نکته‌ها ⚠️

- سقف رایگان: **۱۰۰ دقیقه لینوکس در ماه**. ساخت IL2CPP حدود ۲۰–۶۰ دقیقه است؛ پس چند ساخت در ماه راحت جا می‌شود.
- اگر نام دقیق برچسب‌ها در داشبورد با این جدول فرق داشت، نزدیک‌ترین گزینه را انتخاب کنید یا از همان صفحه عکس بفرستید.
- ساخت‌های بعدی: فقط دکمه‌ی Build — همین.
