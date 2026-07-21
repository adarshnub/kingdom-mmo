using TMPro;
using UnityEngine;

namespace Kingdom.UI
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private Sprite heroBackdrop;

        private readonly string[] tips =
        {
            "March outcomes are calculated by the server.",
            "Alliance help reduces active construction timers.",
            "Scout before committing troops beyond your ward.",
        };

        private ProgressBarView progress;
        private TMP_Text tipText;
        private float elapsed;
        private int lastPercent = -1;

        public void Configure(Sprite backdrop, UiTheme configuredTheme)
        {
            heroBackdrop = backdrop;
            theme = configuredTheme;
        }

        private void Start()
        {
            UiTheme resolved = UiTheme.Resolve(theme);
            CanvasView canvas = UiFactory.CreateCanvas("LoadingCanvas", transform, resolved);
            ScreenUiBuilder.CreateBackground(canvas.Canvas.transform, "Marshal", heroBackdrop, resolved.Colors.Background);
            UiFactory.CreatePanel(canvas.Canvas.transform, "Scrim", new Color(0.01f, 0.03f, 0.05f, 0.48f));
            canvas.SafeArea.SetAsLastSibling();

            TMP_Text title = UiFactory.CreateText(
                canvas.SafeArea,
                "Title",
                "KINGDOM ONE",
                resolved.Typography.Hero,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            UiFactory.SetAnchors(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.86f), Vector4.zero);

            RectTransform loadingCard = UiFactory.CreatePanel(
                canvas.SafeArea,
                "LoadingCard",
                resolved.Colors.Overlay,
                new Vector2(0.06f, 0.045f),
                new Vector2(0.94f, 0.24f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(loadingCard, 18f, new RectOffset(28, 28, 24, 24));
            tipText = UiFactory.CreateText(
                loadingCard,
                "Tip",
                tips[0],
                resolved.Typography.Caption,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Center);
            ScreenUiBuilder.SetLayout(tipText, 82f);
            progress = UiFactory.CreateProgressBar(loadingCard, "Progress", "PREPARING REALM  0%", 0f, resolved);
            progress.Fill.color = resolved.Colors.Gold;
            ScreenUiBuilder.SetLayout(progress.Root, 72f);
        }

        private void Update()
        {
            if (progress == null)
                return;

            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.PingPong(elapsed * 0.18f, 1f);
            int percent = Mathf.RoundToInt(normalized * 100f);
            progress.SetValue(normalized);
            if (percent == lastPercent)
                return;

            lastPercent = percent;
            progress.Label.text = $"PREPARING REALM  {percent}%";
            tipText.text = tips[Mathf.Min(tips.Length - 1, percent * tips.Length / 101)];
        }
    }
}
