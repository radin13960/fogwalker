using System;
using System.Collections;
using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Optimization;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Weapons
{
    /// <summary>
    /// نمونه زمان‌اجرا یک سلاح: مهمات، نرخ آتش، شلیک Hit-scan (با اعتبارسنجی دهانه سلاح)،
    /// پراکندگی+گرمای تیر، لگد دوربین، Reload (غیرقابل لغو برای جلوگیری از خطای مهمات) و بازخوردها.
    /// نکته: Raycast اصلی از مرکز دوربین زده می‌شود و Raycast دوم از Muzzle برای جلوگیری از «شلیک از پشت دیوار».
    /// </summary>
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("داده")]
        [SerializeField] private WeaponDataSO data;

        [Header("سیم‌کشی")]
        [SerializeField, Tooltip("نقطه خروج گلوله/افکت‌ها")]
        private Transform muzzle;

        /// <summary>داده سلاح.</summary>
        public WeaponDataSO Data => data;
        /// <summary>دهانه سلاح.</summary>
        public Transform Muzzle => muzzle;

        /// <summary>مهمات داخل خشاب.</summary>
        public int AmmoInMag { get; private set; }
        /// <summary>مهمات ذخیره.</summary>
        public int ReserveAmmo { get; private set; }
        /// <summary>در حال Reload؟</summary>
        public bool IsReloading { get; private set; }
        /// <summary>در حال تعویض؟</summary>
        public bool IsSwitching { get; private set; }
        /// <summary>پراکندگی مؤثر فعلی (درجه) برای Crosshair پویا.</summary>
        public float CurrentSpread { get; private set; }
        /// <summary>ضرب FOV هنگام Aim برای دوربین.</summary>
        public float AimFovMultiplier => data != null ? data.aimFovMultiplier : 1f;

        /// <summary>گرمای تیرهای پیاپی (درجه اضافی).</summary>
        private float _spreadHeat;
        private float _fireTimer;
        private Camera _ownerCamera;

        // آمار دقت (shots/hits) برای پایان مرحله
        /// <summary>تعداد گلوله‌های شلیک‌شده.</summary>
        public int ShotsFired { get; private set; }
        /// <summary>تعداد گلوله‌هایی که هدف زنده خوردند.</summary>
        public int ShotsHit { get; private set; }

        /// <summary>(خشاب، ذخیره)</summary>
        public event Action<int, int> OnAmmoChanged;
        /// <summary>به‌هنگام شلیک موفق (برای انیمیشن/لرزش).</summary>
        public event Action OnFired;
        /// <summary>شروع/پایان Reload.</summary>
        public event Action<bool> OnReloadStateChanged;

        private const float MuzzleValidationMaxDist = 2.5f; // حداکثر فاصله‌ای که دیوار می‌تواند بین دوربین و دهانه باشد

        /// <summary>راه‌اندازی با داده جدید (در Spawn/سوارشدن).</summary>
        public void Initialize(WeaponDataSO weaponData, Transform muzzleTransform, Camera ownerCamera)
        {
            data = weaponData;
            muzzle = muzzleTransform;
            _ownerCamera = ownerCamera;
            AmmoInMag = data.magazineSize;
            ReserveAmmo = data.reserveStart;
            IsReloading = false;
            IsSwitching = false;
            _fireTimer = 0f;
            _spreadHeat = 0f;
            NotifyAmmo();
        }

        private void Update()
        {
            if (_fireTimer > 0f) _fireTimer -= Time.deltaTime;
            if (_spreadHeat > 0f)
                _spreadHeat = Mathf.Max(0f, _spreadHeat - data.spreadHeatRecovery * Time.deltaTime);
        }

        /// <summary>فراخوانی هر فریم از کنترلر مبارزه؛ مدیریت خودکار/نیمه‌خودکار.</summary>
        /// <param name="triggerHeld">دکمه نگه داشته شده</param>
        /// <param name="triggerPressed">لبه این فریم</param>
        /// <param name="isAiming">حالت Aim فعال؟</param>
        /// <param name="isMoving">بازیکن در حال حرکت سریع است؟</param>
        /// <returns>آیا تیری شلیک شد؟</returns>
        public bool TickFire(bool triggerHeld, bool triggerPressed, bool isAiming, bool isMoving)
        {
            if (data == null) return false;
            bool wantsFire = data.fireMode == FireMode.Auto ? triggerHeld : triggerPressed;
            if (!wantsFire) return false;

            if (AmmoInMag <= 0)
            {
                if (triggerPressed)
                {
                    Audio.AudioManager.PlaySfxShielded(data.emptySfxKey, transform.position);
                    // سیاست UX: خشاب خالی → شروع Reload خودکار
                    TryStartReload();
                }
                return false;
            }

            if (!WeaponMath.CanFire(AmmoInMag, IsReloading, IsSwitching, _fireTimer))
                return false;

            FireOnce(isAiming, isMoving);
            return true;
        }

        /// <summary>شلیک یک دور (یا یک پخش ساچمه برای شاتگان).</summary>
        private void FireOnce(bool isAiming, bool isMoving)
        {
            _fireTimer = 60f / Mathf.Max(1f, data.roundsPerMinute);
            AmmoInMag--;

            float baseSpread = isAiming ? data.spreadAim : data.spreadHip;
            float moveAdd = isMoving ? data.spreadMoveAdd : 0f;
            CurrentSpread = WeaponMath.EffectiveSpread(baseSpread, moveAdd, _spreadHeat);
            _spreadHeat += data.spreadHeatPerShot;

            int pellets = Mathf.Max(1, data.pellets);
            for (int i = 0; i < pellets; i++)
                FireSingleRay(CurrentSpread, i == 0); // فقط ساچمه اول ردیاب بصری دارد (بودجه)

            OnFired?.Invoke();
            NotifyAmmo();
            Audio.AudioManager.PlaySfxShielded(data.fireSfxKey, transform.position);
            AISoundBus.Report(transform.position, 28f, 1f); // شنوایی AI: شلیک بلند
            SpawnMuzzleFlash();
        }

        private void FireSingleRay(float spreadDegrees, bool spawnTracer)
        {
            Camera cam = _ownerCamera != null ? _ownerCamera : Camera.main;
            if (cam == null) return;

            Vector3 origin = cam.transform.position;
            Vector3 dir = WeaponMath.ApplyConeSpread(cam.transform.forward, spreadDegrees);

            Vector3 endPoint = origin + dir * data.maxRange;
            bool hitSomething = false;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, data.maxRange, GameplayLayers.BulletMask, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;
                hitSomething = true;
            }

            // اعتبارسنجی دهانه سلاح: اگر دیوار بین دوربین و دهانه است، شلیک را به نقطه دیوار محدود کن
            if (muzzle != null && Physics.Linecast(origin, muzzle.position, out RaycastHit block, GameplayLayers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                float blockDist = Vector3.Distance(origin, block.point);
                if (blockDist < Vector3.Distance(origin, endPoint) && blockDist <= MuzzleValidationMaxDist)
                {
                    endPoint = block.point;
                    hitSomething = false; // خودمان به دیوار خوردیم؛ آسیب ندارد
                    SpawnImpact(block, null);
                    if (spawnTracer) SpawnTracer(muzzle.position, endPoint);
                    return;
                }
            }

            if (hitSomething)
                ResolveHit(hit, endPoint, spawnTracer);
            else if (spawnTracer)
                SpawnTracer(muzzle != null ? muzzle.position : origin, endPoint);

            ShotsFired++;
        }

        private void ResolveHit(RaycastHit hit, Vector3 endPoint, bool spawnTracer)
        {
            if (spawnTracer && muzzle != null) SpawnTracer(muzzle.position, endPoint);

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                float damage = data.DamageAtDistance(hit.distance);
                damageable.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Type = DamageType.Bullet,
                    HitPoint = endPoint,
                    Direction = (endPoint - (muzzle != null ? muzzle.position : transform.position)).normalized,
                    Instigator = this,
                });
                ShotsHit++;
            }

            SpawnImpact(hit, damageable);
        }

        private void SpawnImpact(RaycastHit hit, IDamageable damaged)
        {
            if (damaged != null) return; // روی بدن، افکت سطحی نزن (خونی نیست؛ تمیزکاری بصری)
            if (!ServiceLocator.TryGet(out PoolManager pool) || ImpactLibrarySource.Library == null) return;

            var tag = hit.collider.GetComponentInParent<SurfaceTag>();
            GameObject prefab = ImpactLibrarySource.Library.Get(tag != null ? tag.Surface : SurfaceType.Concrete);
            if (prefab == null) return;

            Quaternion rot = Quaternion.LookRotation(hit.normal);
            pool.Spawn(prefab, hit.point + hit.normal * 0.01f, rot);
        }

        private void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (data.tracerPrefab == null || !ServiceLocator.TryGet(out PoolManager pool)) return;
            GameObject go = pool.Spawn(data.tracerPrefab, from, Quaternion.identity);
            if (go != null && go.TryGetComponent(out PooledTracer tracer))
                tracer.SetLine(from, to);
        }

        private void SpawnMuzzleFlash()
        {
            if (data.muzzleFlashPrefab == null || muzzle == null || !ServiceLocator.TryGet(out PoolManager pool)) return;
            pool.Spawn(data.muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
        }

        // ---------- Reload ----------

        /// <summary>تلاش برای Reload تاکتیکی/خشاب خالی؛ غیرقابل لغو تا پایان.</summary>
        public bool TryStartReload()
        {
            if (IsReloading || IsSwitching) return false;
            int transfer = WeaponMath.ComputeReloadTransfer(data.magazineSize, AmmoInMag, ReserveAmmo);
            if (transfer <= 0)
            {
                Audio.AudioManager.PlaySfxShielded(data.emptySfxKey, transform.position);
                return false;
            }
            StartCoroutine(ReloadRoutine(transfer));
            return true;
        }

        private IEnumerator ReloadRoutine(int transfer)
        {
            IsReloading = true;
            OnReloadStateChanged?.Invoke(true);
            Audio.AudioManager.PlaySfxShielded(data.reloadSfxKey, transform.position);

            float elapsed = 0f;
            while (elapsed < data.reloadTime)
            {
                elapsed += Time.deltaTime; // در Pause زمان می‌ایستد و Reload هم متوقف می‌ماند (درست)
                yield return null;
            }

            AmmoInMag += transfer;
            ReserveAmmo -= transfer;
            IsReloading = false;
            OnReloadStateChanged?.Invoke(false);
            NotifyAmmo();
        }

        /// <summary>قفل تعویض سلاح (از Inventory هنگام جابه‌جایی).</summary>
        public void SetSwitching(bool switching) => IsSwitching = switching;

        /// <summary>افزودن مهمات ذخیره (Pickup) با سقف.</summary>
        public void AddReserveAmmo(int amount)
        {
            ReserveAmmo = Mathf.Min(data.reserveMax, ReserveAmmo + Mathf.Max(0, amount));
            NotifyAmmo();
        }

        /// <summary>ریست کامل مهمات (برای شروع مرحله/چک‌پوینت).</summary>
        public void ResetAmmo()
        {
            StopAllCoroutines();
            IsReloading = false;
            AmmoInMag = data.magazineSize;
            ReserveAmmo = data.reserveStart;
            NotifyAmmo();
        }

        private void NotifyAmmo() => OnAmmoChanged?.Invoke(AmmoInMag, ReserveAmmo);
    }

    /// <summary>نگهدارنده سراسری ImpactLibrary (توسط GameplayBootstrapper تنظیم می‌شود).</summary>
    public static class ImpactLibrarySource
    {
        public static Combat.ImpactLibrarySO Library { get; set; }
    }
}
