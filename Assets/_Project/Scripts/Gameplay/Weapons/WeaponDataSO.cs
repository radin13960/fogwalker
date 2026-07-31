using UnityEngine;

namespace FogWalker.Gameplay.Weapons
{
    /// <summary>انواع سلاح نسخه اولیه.</summary>
    public enum WeaponType { Pistol, AssaultRifle, SMG, Shotgun, DMR }

    /// <summary>حالت شلیک.</summary>
    public enum FireMode { Semi, Auto }

    /// <summary>
    /// داده کامل یک سلاح (داده‌محور). مقادیر اولیه با کارخانه Setup ساخته می‌شوند.
    /// همه واحدها SI: آسیب به HP، نرخ آتش دور در دقیقه، زمان‌ها به ثانیه.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "FogWalker/Weapons/Weapon Data")]
    public sealed class WeaponDataSO : ScriptableObject
    {
        [Header("هویت")]
        public string weaponId = "pistol";
        public WeaponType type = WeaponType.Pistol;
        [Tooltip("کلید متن نام سلاح در جدول Localization")] public string displayNameKey = "weapon.pistol";

        [Header("آسیب و برد")]
        public float damagePerBullet = 25f;
        [Tooltip("آغاز کاهش آسیب (متر)")] public float falloffStart = 20f;
        [Tooltip("پایان کاهش آسیب (متر)")] public float falloffEnd = 60f;
        [Range(0f, 1f), Tooltip("حداقل ضریب آسیب در برد دور")] public float falloffMinMultiplier = 0.5f;
        public float maxRange = 120f;

        [Header("آتش")]
        public FireMode fireMode = FireMode.Semi;
        [Tooltip("دور در دقیقه")] public float roundsPerMinute = 300f;
        [Tooltip("تعداد ساچمه در هر شلیک (شاتگان)")] public int pellets = 1;

        [Header("مهمات")]
        public int magazineSize = 15;
        public int reserveStart = 60;
        public int reserveMax = 150;
        public float reloadTime = 1.4f;
        public float switchTime = 0.35f;

        [Header("پراکندگی (درجه)")]
        public float spreadHip = 2.5f;
        public float spreadAim = 0.4f;
        [Tooltip("افزایش پراکندگی هنگام حرکت")] public float spreadMoveAdd = 1.2f;
        [Tooltip("افزایش به‌ازای هر تیر پیاپی (گرمایش)")] public float spreadHeatPerShot = 0.25f;
        [Tooltip("سرعت سرد شدن پراکندگی در ثانیه")] public float spreadHeatRecovery = 3f;

        [Header("لگد دوربین")]
        public float recoilPitch = 0.9f;
        public float recoilYawRandom = 0.35f;
        [Tooltip("سرعت بازگشت نرم دوربین")] public float recoilRecovery = 8f;

        [Header("هدف‌گیری")]
        [Range(0.4f, 1f), Tooltip("ضریب FOV هنگام Aim (کمتر = زوم بیشتر)")] public float aimFovMultiplier = 0.8f;

        [Header("بازخورد (پری‌فب‌های Pool‌شده)")]
        public GameObject muzzleFlashPrefab;
        public GameObject tracerPrefab;
        [Header("صدا (کلید در SfxLibrary)")]
        public string fireSfxKey = "sfx.fire.pistol";
        public string reloadSfxKey = "sfx.reload";
        public string emptySfxKey = "sfx.empty";

        /// <summary>آسیب واقعی در فاصله مشخص (با Falloff).</summary>
        public float DamageAtDistance(float distance) => damagePerBullet * WeaponMath.FalloffMultiplier(distance, falloffStart, falloffEnd, falloffMinMultiplier);
    }

    /// <summary>
    /// محاسبات خالص سلاح (بدون وابستگی به GameObject) — قابل تست در EditMode.
    /// </summary>
    public static class WeaponMath
    {
        /// <summary>ضریب Falloff خطی: ۱ تا فاصله شروع، سپس خطی تا حداقل در پایان، بعد از آن ثابت در حداقل.</summary>
        public static float FalloffMultiplier(float distance, float start, float end, float minMultiplier)
        {
            if (distance <= start) return 1f;
            if (end <= start) return minMultiplier;
            if (distance >= end) return minMultiplier;
            return Mathf.Lerp(1f, minMultiplier, (distance - start) / (end - start));
        }

        /// <summary>
        /// پراکندگی مؤثر فعلی (درجه): پایه حالت + افزایش حرکت + گرمای تیرهای پیاپی.
        /// </summary>
        public static float EffectiveSpread(float baseSpread, float moveAdded, float heat, float maxHeatSpread = 8f)
        {
            return Mathf.Min(baseSpread + moveAdded + heat, maxHeatSpread);
        }

        /// <summary>جهت شلیک با پراکندگی مخروطی حول محور دوربین (یونیفرم در زاویه).</summary>
        public static Vector3 ApplyConeSpread(Vector3 forward, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return forward;
            float angleRad = spreadDegrees * Mathf.Deg2Rad;
            float radius = Random.value * Mathf.Tan(angleRad);
            float theta = Random.value * Mathf.PI * 2f;

            // قاعده متعامد
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            Vector3 up = Vector3.Cross(forward, right);

            Vector3 dir = forward + (right * Mathf.Cos(theta) + up * Mathf.Sin(theta)) * radius;
            return dir.normalized;
        }

        /// <summary>آیا مجله‌خالی شلیک را ممنوع می‌کند؟ (برای تست و HUD)</summary>
        public static bool CanFire(int ammoInMag, bool reloading, bool switching, float fireTimer)
        {
            return ammoInMag > 0 && !reloading && !switching && fireTimer <= 0f;
        }

        /// <summary>محاسبه مهمات Reload: هرگز بیش از نیاز از ذخیره برنمی‌دارد.</summary>
        public static int ComputeReloadTransfer(int magSize, int ammoInMag, int reserve)
        {
            int need = magSize - ammoInMag;
            return Mathf.Min(need, reserve);
        }
    }
}
