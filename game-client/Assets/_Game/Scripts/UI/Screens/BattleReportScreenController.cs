using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kingdom.UI
{
    public sealed class BattleReportScreenController : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private Sprite worldBackdrop;

        public void Configure(Sprite backdrop, UiTheme configuredTheme)
        {
            worldBackdrop = backdrop;
            theme = configuredTheme;
        }

        private void Start()
        {
            UiTheme resolved = UiTheme.Resolve(theme);
            CanvasView canvas = UiFactory.CreateCanvas("BattleReportCanvas", transform, resolved);
            ScreenUiBuilder.CreateBackground(canvas.Canvas.transform, "Battlefield", worldBackdrop, resolved.Colors.Background);
            UiFactory.CreatePanel(canvas.Canvas.transform, "Scrim", new Color(0.01f, 0.03f, 0.05f, 0.68f));
            canvas.SafeArea.SetAsLastSibling();

            RectTransform content = UiFactory.CreatePanel(
                canvas.SafeArea,
                "Report",
                Color.clear,
                new Vector2(0.04f, 0.04f),
                new Vector2(0.96f, 0.96f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(content, 18f);

            TMP_Text title = UiFactory.CreateText(
                content,
                "Title",
                "BATTLE REPORT",
                resolved.Typography.Heading,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(title, 86f);
            TMP_Text outcome = UiFactory.CreateText(
                content,
                "Outcome",
                "VICTORY  •  ASHEN SENTINEL DEFEATED",
                resolved.Typography.Title,
                resolved.Colors.GoldBright,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(outcome, 74f);

            RectTransform comparison = UiFactory.CreateCard(content, "Forces", resolved);
            ScreenUiBuilder.SetLayout(comparison, 260f);
            TMP_Text forces = UiFactory.CreateText(
                comparison,
                "ForcesText",
                "MARSHAL ASTER                         ASHEN SENTINEL\nPower 24.8K                                  Power 21.2K\nDeployed 1,000                            Hostiles 860\nWounded 74                                 Defeated 860",
                resolved.Typography.Caption,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            UiFactory.SetStretch(forces.rectTransform, 24f, 20f);

            ProgressBarView health = UiFactory.CreateProgressBar(content, "Remaining", "FORCE REMAINING  82%", 0.82f, resolved);
            ScreenUiBuilder.SetLayout(health.Root, 72f);

            RectTransform rewards = UiFactory.CreateCard(content, "Rewards", resolved);
            ScreenUiBuilder.SetLayout(rewards, 220f);
            TMP_Text rewardText = UiFactory.CreateText(
                rewards,
                "RewardText",
                "REWARDS\nMoonsteel  2.4K    •    Field Rations  6.8K\nMarshal Experience  +1,250    •    Beacon Shard  +1",
                resolved.Typography.Body,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            UiFactory.SetStretch(rewardText.rectTransform, 24f, 20f);

            TMP_Text authority = UiFactory.CreateText(
                content,
                "Authority",
                "Result verified by deterministic server simulation.",
                resolved.Typography.Caption,
                resolved.Colors.TextSecondary,
                TextAlignmentOptions.Center);
            ScreenUiBuilder.SetLayout(authority, 64f);

            ButtonView replay = UiFactory.CreateButton(content, "Replay", "REPLAY FORMATION", resolved, ButtonTone.Secondary);
            replay.Button.onClick.AddListener(() => authority.text = "Replay timeline prototype ready for animation data.");
            ButtonView back = UiFactory.CreateButton(content, "Back", "RETURN TO CITY", resolved);
            back.Button.onClick.AddListener(ReturnToMain);
        }

        private static void ReturnToMain()
        {
            if (Application.CanStreamedLevelBeLoaded("Main"))
                SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
        }
    }
}
