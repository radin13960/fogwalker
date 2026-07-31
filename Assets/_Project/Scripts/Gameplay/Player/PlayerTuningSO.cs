using UnityEngine;

namespace FogWalker.Gameplay.Player
{
    /// <summary>
    /// تمام مقادیر قابل بالانس بازیکن؛ منبع واحد حقیقت برای حرکت/سلامتی/کاور. هیچ Magic Number در کد بازیکن نیست.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerTuning", menuName = "FogWalker/Player/Player Tuning")]
    public sealed class PlayerTuningSO : ScriptableObject
    {
        [Header("حرکت")]
        public float walkSpeed = 3.2f;
        public float runSpeed = 5.8f;
        public float crouchSpeed = 1.8f;
        [Tooltip("ضرب سرعت هنگام Aim")] public float aimSpeedMultiplier = 0.55f;
        public float acceleration = 18f;
        [Tooltip("نرمی چرخش بدن به سمت حرکت (ثانیه)")] public float turnSmoothTime = 0.12f;
        public float gravity = -18f;
        [Tooltip("کم‌هزینه/اختیاری؛ ۰ = غیرفعال")] public float jumpHeight = 1.0f;

        [Header("سلامت (HealthComponent)")]
        public float maxHealth = 100f;
        [Tooltip("بازتولید خودکار محدود؛ ۰ = فقط با کیت درمانی (طراحی فعلی)")] public float regenPerSecond = 0f;
        public float regenDelay = 3f;

        [Header("کاور")]
        public float coverSnapDistance = 2.2f;
        public float coverMoveSpeed = 2.2f;
        public Vector3 coverPeekOffset = new Vector3(0.55f, 0.1f, 0.35f);

        [Header("تعامل")]
        public float interactRadius = 2.2f;

        [Header("نارنجک")]
        public int grenadeStartCount = 2;
        public int grenadeMaxCount = 4;
    }
}
