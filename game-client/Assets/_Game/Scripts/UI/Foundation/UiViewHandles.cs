using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.UI
{
    public enum ButtonTone
    {
        Primary,
        Secondary,
        Danger,
        Quiet
    }

    public sealed class CanvasView
    {
        internal CanvasView(Canvas canvas, RectTransform safeArea, UiTheme theme)
        {
            Canvas = canvas;
            SafeArea = safeArea;
            Theme = theme;
        }

        public Canvas Canvas { get; }
        public RectTransform SafeArea { get; }
        public UiTheme Theme { get; }
    }

    public sealed class ButtonView
    {
        internal ButtonView(RectTransform root, Button button, Image background, TMP_Text label)
        {
            Root = root;
            Button = button;
            Background = background;
            Label = label;
        }

        public RectTransform Root { get; }
        public Button Button { get; }
        public Image Background { get; }
        public TMP_Text Label { get; }
    }

    public sealed class ProgressBarView
    {
        internal ProgressBarView(RectTransform root, Image track, Image fill, TMP_Text label)
        {
            Root = root;
            Track = track;
            Fill = fill;
            Label = label;
        }

        public RectTransform Root { get; }
        public Image Track { get; }
        public Image Fill { get; }
        public TMP_Text Label { get; }

        public void SetValue(float normalizedValue)
        {
            RectTransform fillRect = Fill.rectTransform;
            Vector2 maximum = fillRect.anchorMax;
            maximum.x = Mathf.Clamp01(normalizedValue);
            fillRect.anchorMax = maximum;
        }
    }

    public sealed class ScrollAreaView
    {
        internal ScrollAreaView(
            RectTransform root,
            ScrollRect scrollRect,
            RectTransform viewport,
            RectTransform content)
        {
            Root = root;
            ScrollRect = scrollRect;
            Viewport = viewport;
            Content = content;
        }

        public RectTransform Root { get; }
        public ScrollRect ScrollRect { get; }
        public RectTransform Viewport { get; }
        public RectTransform Content { get; }
    }
}
