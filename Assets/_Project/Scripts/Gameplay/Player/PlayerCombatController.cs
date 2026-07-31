using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Weapons;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Player
{
    /// <summary>
    /// هدایت مبارزه بازیکن: ورودی → سلاح فعال (TickFire)، Aim (اتصال به دوربین و کاهش سرعت)،
    /// Reload، تعویض، نارنجک، کاور، تعامل. آمار شلیک برای پایان مرحله اینجاست.
    /// </summary>
    public sealed class PlayerCombatController : MonoBehaviour
    {
        private PlayerController _player;
        private PlayerCameraController _cameraController;
        private WeaponInventory _inventory;
        private GrenadeThrower _grenades;
        private CoverController _cover;
        private PlayerInteractionScanner _interaction;
        private HealthComponent _health;

        /// <summary>دقت تجمیعی مرحله (0..1) — از روی همه سلاح‌ها.</summary>
        public float Accuracy
        {
            get
            {
                var w = _inventory != null ? _inventory.Active : null;
                // آمار از WeaponControllerها؛ در پایان مرحله MissionManager جمع می‌زند
                return w == null || w.ShotsFired == 0 ? 0f : (float)w.ShotsHit / w.ShotsFired;
            }
        }

        /// <summary>پراکندگی فعلی برای Crosshair.</summary>
        public float CurrentSpreadDegrees => _inventory != null && _inventory.Active != null
            ? _inventory.Active.CurrentSpread : 0f;

        /// <summary>آیا الان در حال Aim است؟</summary>
        public bool IsAiming { get; private set; }

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _cameraController = GetComponent<PlayerCameraController>();
            _inventory = GetComponent<WeaponInventory>();
            _grenades = GetComponent<GrenadeThrower>();
            _cover = GetComponent<CoverController>();
            _interaction = GetComponent<PlayerInteractionScanner>();
            _health = GetComponent<HealthComponent>();
        }

        private void Update()
        {
            if (GameplayInputSource.Current == null) return;
            if (_health != null && !_health.IsAlive) return;

            GameplayInputSource input = GameplayInputSource.Current;
            var weapon = _inventory != null ? _inventory.Active : null;

            // Aim — در کاور هم ممکن (Peek)
            IsAiming = input.AimHeld && weapon != null && !weapon.IsReloading;
            _player.IsAiming = IsAiming;
            if (_cameraController != null)
                _cameraController.SetAiming(IsAiming, weapon != null ? weapon.AimFovMultiplier : 1f);

            // شلیک
            if (weapon != null)
            {
                bool isMoving = _player.PlanarSpeed > _player.Tuning.walkSpeed * 0.6f;
                bool fired = weapon.TickFire(input.FireHeld, input.FirePressed, IsAiming, isMoving);
                if (fired)
                {
                    // لگد دوربین + لرزش کوتاه هپتیک
                    if (_cameraController != null && weapon.Data != null)
                        _cameraController.AddRecoil(
                            weapon.Data.recoilPitch * 0.12f,
                            Random.Range(-weapon.Data.recoilYawRandom, weapon.Data.recoilYawRandom) * 0.12f);
                    HapticsUtility.Short();
                }

                // Reload دستی
                if (input.ReloadPressed)
                    weapon.TryStartReload();
            }

            // تعویض سلاح
            if (input.WeaponCycleDelta != 0 && _inventory != null)
                _inventory.Cycle(input.WeaponCycleDelta);

            // نارنجک
            if (input.GrenadePressed && _grenades != null && _cameraController != null && _cameraController.MainCamera != null)
            {
                Transform cam = _cameraController.MainCamera.transform;
                _grenades.TryThrow(cam.position, cam.forward);
            }

            // کاور
            if (_cover != null && input.CoverPressed && !_cover.IsInCover)
                _cover.TryEnterCover();

            // تعامل
            if (_interaction != null && input.InteractPressed)
                _interaction.TryInteract();
        }
    }
}
