using System;
using System.Collections.Generic;
using FogWalker.Utilities;

namespace FogWalker.Core
{
    /// <summary>
    /// Service Locator سبک و مستند برای سرویس‌های زمان اجرا (جایگزین Singletonهای بی‌رویه).
    /// سرویس‌ها در GameBootstrapper ثبت می‌شوند. برای سرعت، نتیجه Get را کش کنید و در Update تکرار نکنید.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>(16);

        /// <summary>ثبت یک سرویس. ثبت مجدد همان نوع، نسخه قبلی را جایگزین می‌کند.</summary>
        public static void Register<T>(T service) where T : class
        {
            Services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>حذف سرویس (مثلاً هنگام خروج از مرحله برای جلوگیری از مرجع سرگردان).</summary>
        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        /// <summary>بازیابی سرویس؛ در نبود سرویس استثنا با پیام واضح پرتاب می‌کند.</summary>
        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object service))
                return (T)service;

            throw new InvalidOperationException(
                $"[ServiceLocator] سرویس {typeof(T).Name} ثبت نشده است. آیا صحنه را از Bootstrap اجرا کرده‌اید؟");
        }

        /// <summary>تلاش برای بازیابی بدون استثنا.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object raw))
            {
                service = (T)raw;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>نسخه بدون out برای خوانایی در کدهای غیربحرانی.</summary>
        public static T TryGet<T>() where T : class
        {
            return Services.TryGetValue(typeof(T), out object raw) ? (T)raw : null;
        }

        /// <summary>پاک‌سازی کامل — فقط برای تست‌ها و ری‌استارت Bootstrap.</summary>
        public static void Reset()
        {
            Services.Clear();
            GameLog.Info("[ServiceLocator] Reset شد.");
        }
    }
}
