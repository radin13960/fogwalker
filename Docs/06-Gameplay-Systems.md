# ۰۶ — نقشه سیستم‌های گیم‌پلی (فاز ۲–۴)

## جریان یک مرحله
```
SceneLoader → GameplayBootstrapper (تزریق سختی، وصل‌کردن سرویس‌ها، تنظیم بازیکن، بازیابی چک‌پوینت)
    ├── MissionManager ← ObjectiveTracker(اهداف پشت‌سرهم) ─ رویدادها: Reach/Kill/Interact/Pickup/Tick(Defend)
    ├── CheckpointManager ← CheckpointVolume ها (id + objectiveIndex در SaveData)
    ├── SpawnManager/SpawnZone ها (PlayerEnter/OnObjectiveStart) → PoolManager → دشمنان
    ├── Player (Controller·Camera·Combat·Inventory·Grenades·Cover·Interaction·InputSource)
    └── HUDController (سلامت/مهمات/هدف/مارکر/آسیب جهت‌دار/HitMarker/Toast/پنل‌ها)
```

## فایل ←→ مسئولیت (کلیدی)

| حوزه | فایل‌ها | نکته بالانس |
|---|---|---|
| آسیب | Combat/Damage.cs·HealthComponent.cs·Hitbox.cs | ضرایب سختی در `DS_*` |
| سلاح | Weapons/WeaponDataSO.cs·WeaponMath·WeaponController·WeaponInventory·Grenade.cs | ۵ سلاح در `ScriptableObjects/Weapons/WD_*` |
| بازیکن | Player/PlayerController·PlayerCamera·PlayerCombat·Cover·GameplayInputSource | `PlayerTuning_Main` |
| AI | AI/EnemyBrain(FSM)·Perception·Motor·Combat·StateMachine·Archetype | `EA_Rifleman/Rusher/Heavy` |
| مأموریت | Missions/MissionDataSO·ObjectiveTracker(POCO)·MissionManager·Checkpoints·SpawnManager·ProgressUnlocker | `MD_level1..3` |
| صدا | Audio/AudioManager·AudioLibraries.cs | کلیدها در SfxLibrary؛ کلیپ‌ها را خودتان اضافه می‌کنید |
| رابط | UI/HUD/HUDController·TouchControls·Menus/GameFlowScreens·Settings | کلیدها در LocTable_FA |
| بهینه | Optimization/PoolManager·QualityManager·AdaptiveQualityMonitor | پروفایل‌ها `QP_*` |

## تجمیع داده برای ترفند بالانس
- **Magic Number ممنوع:** همه مقادیر حیاتی در SOهاست (Tuning/Weapon/Archetype/Difficulty/Quality).
- **تغییر دشمن فصل جدید** = Arrchetype جدید + سطر Zone در Blueprint (SetupFactory.Levels).
- **تغییر اهداف مرحله** = فقط ویرایش فیلد `objectives` در `MD_*` (یا Blueprint برای صحنه‌های تولیدی).

## محدودیت‌های شناخته‌شده نسخه فعلی
1. ویژوال/انیمیشن Placeholder است (کپسول/جعبه/بدون Ragdoll واقعی) — معماری Animator آماده (پارامترهای SafeAnim استاندارد) و Asset نهایی جایگزین می‌شود بدون تغییر کد.
2. باز کردن دروازه اهرم برق (مرحله ۲/۳) بصری «ناپدید کردن مانع» نیست؛ اهرم هدف را کامل می‌کند و مسیر فیزیکی از قبل باز است (منظقاً مأموریت تغییری نمی‌کند).
3. Adaptive Quality فقط Render Scale را تنظیم می‌کند (پارامترهای بعدی — LODBias/Particles — آماده متصل‌کردن‌اند).
4. صداها: کتابخانه کلیدها کامل است اما کلیپ‌ها (SFX/Music) باید وارد شوند تا شنیده شوند.
5. Checkpoint دشمنان زنده را ریست نمی‌کند (طراحی: ادامه از چک‌پوینت در همان لحظه مرگ معنی‌دار است؛ اگر خواستید DespawnAll را هنگام مرگ صدا بزنید).
