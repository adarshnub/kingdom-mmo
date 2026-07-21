using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.UI
{
    /// <summary>
    /// Small runtime construction helpers used by generated scenes and early UI slices.
    /// Every created control uses portrait-safe sizing and semantic theme values.
    /// </summary>
    public static class UiFactory
    {
        public static CanvasView CreateCanvas(
            string name,
            Transform parent = null,
            UiTheme theme = null)
        {
            UiTheme resolvedTheme = UiTheme.Resolve(theme);
            EnsureUiCamera(resolvedTheme);
            GameObject canvasObject = CreateUiObject(name, parent);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolvedTheme.Spacing.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = resolvedTheme.Spacing.CanvasWidthHeightMatch;
            scaler.referencePixelsPerUnit = 100f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            SetStretch(canvasRect);

            RectTransform safeArea = CreateRectTransform(canvasObject.transform, "SafeArea");
            SetStretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            return new CanvasView(canvas, safeArea, resolvedTheme);
        }

        private static void EnsureUiCamera(UiTheme theme)
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.Colors.Background;
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.depth = -100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            Image image = CreateImage(parent, name, color);
            SetStretch(image.rectTransform);
            return image.rectTransform;
        }

        /// <summary>
        /// Offsets are ordered left, bottom, right, top.
        /// </summary>
        public static RectTransform CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector4 offsets)
        {
            Image image = CreateImage(parent, name, color);
            SetAnchors(image.rectTransform, anchorMin, anchorMax, offsets);
            return image.rectTransform;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateRectTransform(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft,
            FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = CreateRectTransform(parent, name);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            SetStretch(rect);
            return text;
        }

        public static ButtonView CreateButton(
            Transform parent,
            string name,
            string label,
            UiTheme theme = null,
            ButtonTone tone = ButtonTone.Primary)
        {
            UiTheme resolvedTheme = UiTheme.Resolve(theme);
            UiColors colors = resolvedTheme.Colors;

            Color backgroundColor;
            Color textColor;
            switch (tone)
            {
                case ButtonTone.Secondary:
                    backgroundColor = colors.RoyalBlue;
                    textColor = colors.TextPrimary;
                    break;
                case ButtonTone.Danger:
                    backgroundColor = colors.Danger;
                    textColor = colors.TextPrimary;
                    break;
                case ButtonTone.Quiet:
                    backgroundColor = colors.SurfaceRaised;
                    textColor = colors.TextPrimary;
                    break;
                default:
                    backgroundColor = colors.Gold;
                    textColor = colors.TextOnLight;
                    break;
            }

            Image background = CreateImage(parent, name, backgroundColor);
            RectTransform root = background.rectTransform;
            root.sizeDelta = new Vector2(0f, resolvedTheme.Spacing.MinimumTouchTarget);

            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = colors.Divider;
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = Color.white;
            colorBlock.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colorBlock.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colorBlock.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colorBlock.disabledColor = colors.Disabled;
            colorBlock.colorMultiplier = 1f;
            colorBlock.fadeDuration = 0.08f;
            button.colors = colorBlock;

            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = resolvedTheme.Spacing.MinimumTouchTarget;
            layout.preferredHeight = resolvedTheme.Spacing.MinimumTouchTarget;

            PressFeedback feedback = root.gameObject.AddComponent<PressFeedback>();
            feedback.Configure(resolvedTheme.Motion);

            TMP_Text labelText = CreateText(
                root,
                "Label",
                label,
                resolvedTheme.Typography.Button,
                textColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ApplyFont(labelText, resolvedTheme.Typography.DisplayFont);
            SetStretch(labelText.rectTransform, resolvedTheme.Spacing.Medium, resolvedTheme.Spacing.Small);

            return new ButtonView(root, button, background, labelText);
        }

        public static ButtonView CreateIconButton(
            Transform parent,
            string name,
            string label,
            Sprite icon,
            UiTheme theme = null)
        {
            UiTheme resolvedTheme = UiTheme.Resolve(theme);
            Image background = CreateImage(parent, name, resolvedTheme.Colors.SurfaceRaised);
            RectTransform root = background.rectTransform;

            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = resolvedTheme.Colors.Divider;
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = resolvedTheme.Colors.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            PressFeedback feedback = root.gameObject.AddComponent<PressFeedback>();
            feedback.Configure(resolvedTheme.Motion);

            Image iconImage = CreateImage(root, "Icon", icon != null ? Color.white : resolvedTheme.Colors.Disabled);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            SetAnchors(iconImage.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.98f), Vector4.zero);

            TMP_Text labelText = CreateText(
                root,
                "Label",
                label,
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ApplyFont(labelText, resolvedTheme.Typography.DisplayFont);
            SetAnchors(labelText.rectTransform, new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.27f), Vector4.zero);

            return new ButtonView(root, button, background, labelText);
        }

        public static ProgressBarView CreateProgressBar(
            Transform parent,
            string name,
            string label,
            float normalized,
            UiTheme theme = null)
        {
            UiTheme resolvedTheme = UiTheme.Resolve(theme);
            RectTransform root = CreateRectTransform(parent, name);
            root.sizeDelta = new Vector2(0f, 72f);

            Image track = root.gameObject.AddComponent<Image>();
            track.color = resolvedTheme.Colors.Surface;
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = resolvedTheme.Colors.Divider;
            outline.effectDistance = new Vector2(2f, -2f);

            Image fill = CreateImage(root, "Fill", resolvedTheme.Colors.Success);
            SetAnchors(fill.rectTransform, Vector2.zero, Vector2.one, new Vector4(6f, 6f, 6f, 6f));

            TMP_Text labelText = CreateText(
                root,
                "Label",
                label,
                resolvedTheme.Typography.Caption,
                resolvedTheme.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ApplyFont(labelText, resolvedTheme.Typography.RegularFont);
            SetStretch(labelText.rectTransform, resolvedTheme.Spacing.Small, 0f);

            ProgressBarView view = new ProgressBarView(root, track, fill, labelText);
            view.SetValue(normalized);
            return view;
        }

        public static RectTransform CreateCard(
            Transform parent,
            string name,
            UiTheme theme = null)
        {
            UiTheme resolvedTheme = UiTheme.Resolve(theme);
            Image image = CreateImage(parent, name, resolvedTheme.Colors.SurfaceRaised);
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = resolvedTheme.Colors.Divider;
            outline.effectDistance = new Vector2(2f, -2f);
            return image.rectTransform;
        }

        public static ScrollAreaView CreateScrollArea(
            Transform parent,
            string name,
            UiTheme theme = null)
        {
            UiTheme resolvedTheme = UiTheme.Resolve(theme);
            Image rootImage = CreateImage(parent, name, Color.clear);
            RectTransform root = rootImage.rectTransform;
            SetStretch(root);

            ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.08f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 50f;

            RectTransform viewport = CreateRectTransform(root, "Viewport");
            SetStretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRectTransform(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                Mathf.RoundToInt(resolvedTheme.Spacing.Medium),
                Mathf.RoundToInt(resolvedTheme.Spacing.Medium),
                Mathf.RoundToInt(resolvedTheme.Spacing.Medium),
                Mathf.RoundToInt(resolvedTheme.Spacing.Medium));
            layout.spacing = resolvedTheme.Spacing.Small;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return new ScrollAreaView(root, scrollRect, viewport, content);
        }

        public static void SetStretch(
            RectTransform rect,
            float horizontalPadding = 0f,
            float verticalPadding = 0f)
        {
            SetAnchors(
                rect,
                Vector2.zero,
                Vector2.one,
                new Vector4(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding));
        }

        /// <summary>
        /// Offsets are ordered left, bottom, right, top.
        /// </summary>
        public static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector4 offsets)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(offsets.x, offsets.y);
            rect.offsetMax = new Vector2(-offsets.z, -offsets.w);
        }

        private static RectTransform CreateRectTransform(Transform parent, string name)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            return gameObject.GetComponent<RectTransform>();
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
        {
            if (font != null)
            {
                text.font = font;
            }
        }
    }
}
