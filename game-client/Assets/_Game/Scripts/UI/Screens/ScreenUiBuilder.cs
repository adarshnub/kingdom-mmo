using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.UI
{
    internal static class ScreenUiBuilder
    {
        public static Image CreateBackground(Transform parent, string name, Sprite sprite, Color fallback)
        {
            Image image = UiFactory.CreateImage(parent, name, sprite != null ? Color.white : fallback);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            UiFactory.SetStretch(image.rectTransform);
            image.rectTransform.SetAsFirstSibling();
            return image;
        }

        public static VerticalLayoutGroup AddVerticalLayout(
            RectTransform rect,
            float spacing,
            RectOffset padding = null,
            TextAnchor alignment = TextAnchor.UpperCenter)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding ?? new RectOffset();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(
            RectTransform rect,
            float spacing,
            RectOffset padding = null,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding ?? new RectOffset();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static LayoutElement SetLayout(
            Component component,
            float preferredHeight = -1f,
            float preferredWidth = -1f,
            float flexibleWidth = -1f,
            float flexibleHeight = -1f)
        {
            LayoutElement layout = component.gameObject.GetComponent<LayoutElement>();
            if (layout == null)
                layout = component.gameObject.AddComponent<LayoutElement>();
            if (preferredHeight >= 0f)
                layout.preferredHeight = preferredHeight;
            if (preferredWidth >= 0f)
                layout.preferredWidth = preferredWidth;
            if (flexibleWidth >= 0f)
                layout.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f)
                layout.flexibleHeight = flexibleHeight;
            return layout;
        }

        public static TMP_InputField CreateInput(
            Transform parent,
            string name,
            string placeholder,
            UiTheme theme)
        {
            RectTransform root = UiFactory.CreateCard(parent, name, theme);
            SetLayout(root, 112f);

            RectTransform viewport = UiFactory.CreatePanel(
                root,
                "TextArea",
                Color.clear,
                Vector2.zero,
                Vector2.one,
                new Vector4(32f, 12f, 32f, 12f));
            viewport.gameObject.AddComponent<RectMask2D>();

            TMP_Text text = UiFactory.CreateText(
                viewport,
                "Text",
                string.Empty,
                theme.Typography.Body,
                theme.Colors.TextPrimary,
                TextAlignmentOptions.MidlineLeft);
            TMP_Text placeholderText = UiFactory.CreateText(
                viewport,
                "Placeholder",
                placeholder,
                theme.Typography.Body,
                theme.Colors.TextSecondary,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);

            TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.characterLimit = 24;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.selectionColor = new Color(
                theme.Colors.Gold.r,
                theme.Colors.Gold.g,
                theme.Colors.Gold.b,
                0.45f);
            return input;
        }

        public static RectTransform CreateBadge(
            Transform parent,
            string code,
            UiTheme theme,
            Color color,
            float size = 72f)
        {
            Image badge = UiFactory.CreateImage(parent, $"Badge_{code}", color);
            SetLayout(badge, size, size, 0f, 0f);
            TMP_Text text = UiFactory.CreateText(
                badge.transform,
                "Code",
                code,
                theme.Typography.Caption,
                theme.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            UiFactory.SetStretch(text.rectTransform);
            return badge.rectTransform;
        }

        public static string CompactNumber(string value)
        {
            if (!decimal.TryParse(value, out decimal parsed))
                return string.IsNullOrWhiteSpace(value) ? "0" : value;
            if (parsed >= 1_000_000_000m)
                return $"{parsed / 1_000_000_000m:0.#}B";
            if (parsed >= 1_000_000m)
                return $"{parsed / 1_000_000m:0.#}M";
            if (parsed >= 1_000m)
                return $"{parsed / 1_000m:0.#}K";
            return parsed.ToString("0");
        }

        public static void DestroyChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
        }

        public static string InstallationId()
        {
            const string key = "kingdom.installation-id";
            string value = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            value = Guid.NewGuid().ToString("D");
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            return value;
        }
    }
}
