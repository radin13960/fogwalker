using FogWalker.Core;
using FogWalker.Localization;
using FogWalker.Save;
using FogWalker.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.UI.MainMenu
{
    /// <summary>
    /// پنل انتخاب مرحله؛ آیتم‌ها به‌صورت پویا از SceneCatalog ساخته می‌شوند (افزودن مرحله = افزودن Entry در Catalog).
    /// قفل/بازبودن از SaveData خوانده می‌شود؛ اگر هنوز هیچ مرحله‌ای باز نشده، مرحله اول همیشه باز است.
    /// </summary>
    public sealed class LevelSelectPanel : MonoBehaviour
    {
        [Header("سیم‌کشی")]
        [SerializeField, Tooltip("کانتینر آیتم‌ها (VerticalLayoutGroup توصیه می‌شود)")]
        private Transform contentParent;

        [SerializeField, Tooltip("پری‌فب دکمه آیتم با TMP_Text فرزند")]
        private Button itemPrefab;

        [SerializeField] private Button backButton;

        private ISaveSystem _save;
        private SceneLoader _sceneLoader;
        private LocalizationManager _localization;
        private bool _bound;

        private void Awake()
        {
            if (backButton != null && !_bound)
            {
                backButton.onClick.AddListener(Close);
                _bound = true;
            }
        }

        /// <summary>نمایش پنل و بازسازی فهرست.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            Rebuild();
        }

        /// <summary>بستن پنل.</summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void Rebuild()
        {
            if (!ServiceLocator.TryGet(out _save) || !ServiceLocator.TryGet(out _sceneLoader))
            {
                GameLog.Error("[LevelSelect] سرویس‌ها آماده نیستند.");
                return;
            }
            ServiceLocator.TryGet(out _localization);

            if (contentParent == null || itemPrefab == null)
            {
                GameLog.Error("[LevelSelect] contentParent یا itemPrefab سیم نشده است!");
                return;
            }

            ClearChildren();

            SceneCatalog catalog = _sceneLoader.Catalog;
            if (catalog == null || catalog.levels == null || catalog.levels.Length == 0)
            {
                GameLog.Warn("[LevelSelect] کاتالوگ مرحله‌ای ندارد.");
                return;
            }

            var progress = _save.Data.progress;
            bool anyUnlocked = progress.unlockedLevelIds != null && progress.unlockedLevelIds.Count > 0;
            SceneCatalog.LevelEntry first = catalog.GetFirstLevel();

            foreach (SceneCatalog.LevelEntry entry in catalog.levels)
            {
                if (entry == null) continue;

                bool unlocked = !anyUnlocked
                    ? entry == first
                    : progress.unlockedLevelIds.Contains(entry.levelId);

                CreateItem(entry, unlocked);
            }
        }

        private void CreateItem(SceneCatalog.LevelEntry entry, bool unlocked)
        {
            Button item = Instantiate(itemPrefab, contentParent);
            item.interactable = unlocked;

            TMP_Text label = item.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                string name = GetLocalized(entry.displayNameKey);
                if (!unlocked) name += " — " + GetLocalized("common.locked");
                if (IsManualRtlActive()) { name = PersianTextUtility.Fix(name); label.isRightToLeftText = false; }
                label.text = name;
            }

            if (unlocked)
            {
                string id = entry.levelId;
                item.onClick.AddListener(() =>
                {
                    _sceneLoader.LoadLevelById(id);
                    Close();
                });
            }
        }

        private string GetLocalized(string key)
        {
            return _localization != null ? _localization.GetText(key) : key;
        }

        private bool IsManualRtlActive()
        {
            return _localization != null && _localization.UseBuiltInRtlFix && _localization.CurrentLanguage == "fa";
        }

        private void ClearChildren()
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Transform child = contentParent.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying) { DestroyImmediate(child.gameObject); continue; }
#endif
                Destroy(child.gameObject);
            }
        }
    }
}
