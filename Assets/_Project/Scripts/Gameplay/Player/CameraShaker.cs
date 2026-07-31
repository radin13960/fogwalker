using UnityEngine;

namespace FogWalker.Gameplay.Player.Controllers
{
    /// <summary>
    /// لرزش دوربین کم‌هزینه (بدون Coroutineهای روشن دائم): مقدار Impulse انباشته می‌شود و
    /// به‌صورت نویز نرم روی روت اعمال می‌شود. قابل غیرفعال‌سازی از تنظیمات (cameraShake).
    /// استفاده: CameraShaker.AddProximityImpulse(موقعیت انفجار، شعاع) یا AddImpulse(قدرت).
    /// </summary>
    public static class CameraShaker
    {
        private static float _power;
        private static Vector3 _offset;
        private static Transform _playerTransform;

        /// <summary>ثبت مرجع بازیکن برای شدت فاصله‌محور.</summary>
        public static void RegisterPlayer(Transform player) => _playerTransform = player;

        /// <summary>افزودن ضربه خام (0..1).</summary>
        public static void AddImpulse(float power)
        {
            _power = Mathf.Min(1f, _power + power);
        }

        /// <summary>ضربه فاصله‌محور؛ نزدیک‌تر = قوی‌تر (انفجار).</summary>
        public static void AddProximityImpulse(Vector3 worldPos, float radius)
        {
            if (_playerTransform == null) return;
            float dist = Vector3.Distance(worldPos, _playerTransform.position);
            if (dist > radius) return;
            AddImpulse(Mathf.Lerp(0.6f, 0.1f, dist / radius));
        }

        /// <summary>اعمال در LateUpdate دوربین (وقتی هپتیک/تنظیمات اجازه دهد).</summary>
        internal static void ApplyTick(Transform cam, Quaternion baseRot)
        {
            _power = Mathf.Max(0f, _power - Time.deltaTime * 2.4f);
            if (_power <= 0.001f) { _offset = Vector3.zero; return; }

            float t = Time.time * 23f;
            _offset = new Vector3(
                (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f,
                0f) * (_power * 0.6f);

            cam.rotation = baseRot * Quaternion.Euler(_offset.x * 2.2f, _offset.y * 2.2f, _offset.x);
        }

        /// <summary>ریست نرم وقتی لرزش خاموش است.</summary>
        internal static void ResetTick(Transform cam, Quaternion baseRot)
        {
            if (_power > 0f) _power = Mathf.Max(0f, _power - Time.deltaTime * 4f);
        }
    }
}
