using FogWalker.Core;
using FogWalker.Settings;
using UnityEngine;

namespace FogWalker.Utilities
{
    /// <summary>
    /// لرزش لمسی (Haptic) کوتاه و اختیاری؛ از تنظیمات کاربر اطاعت می‌کند و در Editor بی‌اثر است.
    /// </summary>
    public static class HapticsUtility
    {
        /// <summary>لرزش کوتاه استاندارد (شلیک/برخورد/انفجار نزدیک).</summary>
        public static void Short()
        {
            if (!IsEnabled()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch { }
#endif
        }

        /// <summary>آیا در تنظیمات فعال است؟</summary>
        private static bool IsEnabled()
        {
            return ServiceLocator.TryGet(out SettingsManager settings) &&
                   settings.Data != null && settings.Data.haptics;
        }
    }
}
