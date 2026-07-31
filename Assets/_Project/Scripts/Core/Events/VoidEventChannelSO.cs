using System;
using UnityEngine;

namespace FogWalker.Core.Events
{
    /// <summary>
    /// کانال رویداد بدون داده مبتنی بر ScriptableObject برای اتصال آزاد سیستم‌ها به UI.
    /// الگو: سیستم ناشر Raise می‌کند؛ گیرنده‌ها در OnEnable/OnDisable subscribe/unsubscribe می‌کنند.
    /// کانال‌های دارای داده (Health، Ammo و...) در فاز ۲ با همین الگو اضافه می‌شوند.
    /// </summary>
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "FogWalker/Events/Void Event Channel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        /// <summary>مشترکین رویداد. مدیریت عضویت بر عهده مصرف‌کننده است (جلوگیری از Leak).</summary>
        public event Action OnRaised;

        /// <summary>انتشار رویداد به همه مشترکین.</summary>
        public void Raise() => OnRaised?.Invoke();
    }
}
