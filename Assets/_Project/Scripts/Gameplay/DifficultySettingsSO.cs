using UnityEngine;

namespace FogWalker.Gameplay
{
    /// <summary>
    /// داده درجه سختی؛ در فاز ۱ فقط تعریف و نمونه‌ها (Setup > 2) ساخته می‌شوند
    /// و در فاز ۲+ توسط AI/Damage/Spawn خوانده می‌شود.
    /// همه ضرایب داده‌محور — بدون هیچ Magic Number در کد AI.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultySettings", menuName = "FogWalker/Difficulty/Difficulty Settings")]
    public sealed class DifficultySettingsSO : ScriptableObject
    {
        [Header("هویت")]
        public int difficultyIndex = 1;                    // 0=آسان 1=عادی 2=سخت
        [Tooltip("کلید متن نام سختی در LocalizationTable")] public string displayNameKey = "difficulty.normal";

        [Header("دشمن")]
        [Range(0.5f, 2f)] public float enemyHealthMultiplier = 1f;
        [Range(0.5f, 2f)] public float enemyDamageMultiplier = 1f;
        [Range(0f, 1f), Tooltip("دقت پایه تیراندازی AI؛ بیشتر = منصفانه‌تر/سخت‌تر")] public float enemyBaseAccuracy = 0.55f;
        [Range(0.1f, 2f), Tooltip("سرعت واکنش AI به دیدن بازیکن (ثانیه معکوس ضرب می‌شود)")] public float enemyReactionScale = 1f;

        [Header("منابع بازیکن")]
        [Range(0.5f, 2f)] public float ammoPickupMultiplier = 1f;
        [Range(0.5f, 2f)] public float medkitPickupMultiplier = 1f;
    }
}
