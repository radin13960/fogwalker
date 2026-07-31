using System;
using UnityEngine;

namespace FogWalker.Gameplay.Missions
{
    /// <summary>انواع هدف.</summary>
    public enum ObjectiveType
    {
        /// <summary>رسیدن به نقطه (TriggerVolume/ExtractionZone با همان id).</summary>
        Reach,
        /// <summary>حذف گروه دشمن (SpawnGroupId) تا تعداد مشخص.</summary>
        EliminateGroup,
        /// <summary>تعامل با یک Interactable خاص (id).</summary>
        Interact,
        /// <summary>جمع‌آوری N آیتم مأموریت (id مشترک).</summary>
        Collect,
        /// <summary>دفاع از نقطه برای مدت مشخص (ثانیه) — ماندن در محدوده لازم نیست، فقط زنده‌ماندن هدف.</summary>
        Defend,
    }

    /// <summary>تعریف یک هدف مرحله.</summary>
    [Serializable]
    public sealed class ObjectiveDef
    {
        [Tooltip("شناسه یکتا — باید با Triggerها/Interactableها/AITargetها یکی باشد")] public string id = "obj1";
        public ObjectiveType type = ObjectiveType.Reach;
        [Tooltip("کلید متن در جدول Localization")] public string titleKey = "obj.l1.1";
        [Tooltip("برای Collect: تعداد لازم")] public int requiredCount = 1;
        [Tooltip("برای Defend: مدت (ثانیه)")] public float timeSeconds = 45f;
        [Tooltip("برای EliminateGroup: SpawnGroupId هدف")] public string targetGroupId;
        [Tooltip("نمایش نشانگر جهت/مارکر روی HUD")] public bool showMarker = true;
        [Tooltip("Anchor صحنه برای مارکر (TransformId می‌شود با ObjectiveAnchor)")] public string markerAnchorId;
    }

    /// <summary>داده یک مأموریت/مرحله (داده‌محور).</summary>
    [CreateAssetMenu(fileName = "MissionData", menuName = "FogWalker/Missions/Mission Data")]
    public sealed class MissionDataSO : ScriptableObject
    {
        [Tooltip("باید با levelId در SceneCatalog یکی باشد")]
        public string levelId = "level1";
        public ObjectiveDef[] objectives = Array.Empty<ObjectiveDef>();
        [Tooltip("صدا/مود موسیقی پیش‌فرض مرحله")] public string defaultMusicMood = "Exploration";
    }
}
