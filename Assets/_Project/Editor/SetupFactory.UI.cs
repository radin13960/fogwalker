using FogWalker.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// ابزار ساخت UI برنامه‌نویسی‌شده برای SetupFactory: پالت رنگ، ساخت Rect/Text/Image/Button/Slider/Toggle/Dropdown.
    /// همه متن‌ها با LocalizedText به کلید وصل می‌شوند (بدون Hardcode فارسی در UI).
    /// </summary>
    public static partial class SetupFactory
    {
        // پالت رنگ UI (خوانایی بالا روی تیره)
        public static class UiColors
        {
            public static readonly Color BgDark = new Color(0.08f, 0.09f, 0.12f, 0.94f);
            public static readonly Color Panel = new Color(0.12f, 0.13f, 0.17f, 0.96f);
            public static readonly Color Accent = new Color(0.95f, 0.55f, 0.15f, 1f);
            public static readonly Color Btn = new Color(0.20f, 0.22f, 0.28f, 1f);
            public static readonly Color BtnHigh = new Color(0.30f, 0.33f, 0.42f, 1f);
            public static readonly Color Text = new Color(0.95f, 0.95f, 0.96f, 1f);
            public static readonly Color TextDim = new Color(0.7f, 0.72f, 0.76f, 1f);
            public static readonly Color Health = new Color(0.85f, 0.2f, 0.2f, 1f);
            public static readonly Color HealthBg = new Color(0.2f, 0.05f, 0.05f, 0.8f);
            public static readonly Color Touch = new Color(1f, 1f, 1f, 0.35f);
            public static readonly Color TouchBtn = new Color(0.15f, 0.16f, 0.2f, 0.55f);
        }

        private static TMP_FontAsset _cachedFont;

        /// <summary>فونت TMP برای ساخت UI (فارسی در صورت وجود).</summary>
        public static TMP_FontAsset ResolveFont()
        {
            if (_cachedFont != null) return _cachedFont;
            _cachedFont = TMP_Settings.defaultFontAsset;
            if (_cachedFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                    _cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return _cachedFont;
        }

        /// <summary>ساخت RectTransform فرزند.</summary>
        public static RectTransform NewRect(string name, Transform parent, Vector2 anchoredPos, Vector2 size,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>ساخت Image.</summary>
        public static Image NewImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = true)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>ساخت متن TMP با LocalizedText در صورت داشتن کلید.</summary>
        public static TMP_Text NewText(string name, Transform parent, Vector2 pos, Vector2 size, string keyOrText,
            float fontSize = 26f, Color? color = null, bool localize = true,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = NewRect(name, parent, pos, size);
            var txt = rt.gameObject.AddComponent<TextMeshProUGUI>();
            txt.fontSize = fontSize;
            txt.color = color ?? UiColors.Text;
            txt.alignment = align;
            txt.enableWordWrapping = true;
            if (ResolveFont() != null) txt.font = ResolveFont();
            if (localize)
            {
                var lt = rt.gameObject.AddComponent<LocalizedText>();
                SetField(lt, "key", keyOrText);
            }
            else
            {
                txt.text = keyOrText;
            }
            return txt;
        }

        /// <summary>ساخت دکمه با برچسب محلی‌سازی‌شده.</summary>
        public static Button NewButton(string name, Transform parent, Vector2 pos, Vector2 size, string labelKey,
            Color? bg = null, float fontSize = 26f)
        {
            var rt = NewRect(name, parent, pos, size);
            var img = NewImage(rt, bg ?? UiColors.Btn);
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = UiColors.BtnHigh;
            colors.pressedColor = UiColors.Accent;
            btn.colors = colors;
            NewText("Label", rt, Vector2.zero, size, labelKey, fontSize, UiColors.Text);
            return btn;
        }

        /// <summary>ساخت Slider خام (برای اسلایدرهای تنظیمات/نوار سلامت).</summary>
        public static Slider NewSlider(string name, Transform parent, Vector2 pos, Vector2 size, float min, float max, float value)
        {
            var rt = NewRect(name, parent, pos, size);
            var bg = NewRect("Background", rt, Vector2.zero, size);
            NewImage(bg, new Color(0.05f, 0.05f, 0.07f, 0.9f));
            var fillArea = NewRect("Fill Area", rt, Vector2.zero, size);
            var fill = NewRect("Fill", fillArea, Vector2.zero, size, Vector2.zero, Vector2.one);
            var fillImg = NewImage(fill, UiColors.Accent);
            var handleArea = NewRect("Handle Slide Area", rt, Vector2.zero, size);
            var handle = NewRect("Handle", handleArea, Vector2.zero, new Vector2(18f, size.y + 10f), new Vector2(0.5f, 0.5f));
            NewImage(handle, Color.white);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min; slider.maxValue = max; slider.value = value;
            fillImg.type = Image.Type.Simple;
            return slider;
        }

        /// <summary>ساخت Toggle خام با برچسب.</summary>
        public static Toggle NewToggle(string name, Transform parent, Vector2 pos, string labelKey, bool value)
        {
            var rt = NewRect(name, parent, pos, new Vector2(420f, 44f));
            var box = NewRect("Box", rt, new Vector2(0f, 0f), new Vector2(32f, 32f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            NewImage(box, UiColors.Btn);
            var check = NewRect("Check", box, Vector2.zero, new Vector2(22f, 22f));
            var checkImg = NewImage(check, UiColors.Accent);
            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.graphic = checkImg;
            toggle.isOn = value;
            NewText("Label", rt, new Vector2(230f, 0f), new Vector2(380f, 44f), labelKey, 24f, UiColors.Text);
            return toggle;
        }

        /// <summary>ساخت Dropdown خام TMP (گزینه‌ها بعداً از کد).</summary>
        public static TMP_Dropdown NewDropdown(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = NewRect(name, parent, pos, size);
            NewImage(rt, UiColors.Btn);
            var caption = NewText("Label", rt, Vector2.zero, size, "-", 24f, UiColors.Text, localize: false);
            caption.enableWordWrapping = false;

            var templateRt = NewRect("Template", rt, Vector2.zero, new Vector2(size.x, 30f));
            NewImage(templateRt, UiColors.Panel);
            itemRects(templateRt);
            templateRt.gameObject.SetActive(false);

            var viewport = templateRt.Find("Viewport");
            var content = templateRt.Find("Viewport/Content");
            var item = content != null ? content.Find("Item") : null;
            TMP_Text itemText = item != null ? item.Find("Item Label")?.GetComponent<TMP_Text>() : null;

            var dd = rt.gameObject.AddComponent<TMP_Dropdown>();
            dd.captionText = caption;
            dd.itemText = itemText;
            dd.template = templateRt;
            return dd;

            void itemRects(RectTransform tpl)
            {
                var vp = NewRect("Viewport", tpl, Vector2.zero, tpl.sizeDelta, Vector2.zero, Vector2.one);
                var scrollContent = NewRect("Content", vp, Vector2.zero, tpl.sizeDelta, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                var it = NewRect("Item", scrollContent, Vector2.zero, new Vector2(tpl.sizeDelta.x, 30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                var itImg = NewImage(it, UiColors.Btn);
                // Dropdown به Toggle روی آیتم نیاز دارد
                var itToggle = it.gameObject.AddComponent<Toggle>();
                itToggle.targetGraphic = itImg;
                var nav = itToggle.navigation;
                nav.mode = UnityEngine.UI.Selectable.Mode.Navigation.None;
                itToggle.navigation = nav;
                var label = NewRect("Item Label", it, Vector2.zero, new Vector2(tpl.sizeDelta.x, 30f));
                var t = label.gameObject.AddComponent<TextMeshProUGUI>();
                t.fontSize = 22f; t.color = UiColors.Text; t.alignment = TextAlignmentOptions.Center;
                if (ResolveFont() != null) t.font = ResolveFont();
            }
        }

        /// <summary>ساخت پنل محاوره‌ای مرکزی.</summary>
        public static RectTransform NewDialogPanel(string name, Transform parent, Vector2 size)
        {
            // Dim کل صفحه
            var dim = NewRect(name + "_Dim", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            dim.offsetMin = Vector2.zero; dim.offsetMax = Vector2.zero;
            NewImage(dim, new Color(0f, 0f, 0f, 0.72f));
            var panel = NewRect(name + "_Panel", dim, Vector2.zero, size);
            NewImage(panel, UiColors.Panel);
            return dim;
        }
    }
}
