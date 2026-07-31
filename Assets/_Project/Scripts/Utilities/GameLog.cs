using UnityEngine;

namespace FogWalker.Utilities
{
    /// <summary>
    /// لاگ‌گیری یکپارچه پروژه: Info/Warn فقط در Editor و Development Build کامپایل می‌شوند
    /// (Conditional Compilation) تا بیلد نهایی از هزینه و لو‌رفتن لاگ مصون بماند. Error همیشه فعال است.
    /// قانون پروژه: از Debug.Log مستقیم استفاده نکنید؛ فقط GameLog.
    /// </summary>
    public static class GameLog
    {
        private const string Tag = "[FW] ";

        /// <summary>پیام توسعه — در بیلد Release حذف می‌شود.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message) => Debug.Log(Tag + message);

        /// <summary>هشدار توسعه — در بیلد Release حذف می‌شود.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string message) => Debug.LogWarning(Tag + message);

        /// <summary>خطای مهم — همیشه ثبت می‌شود (کرش‌زا/داده خراب).</summary>
        public static void Error(string message) => Debug.LogError(Tag + message);
    }
}
