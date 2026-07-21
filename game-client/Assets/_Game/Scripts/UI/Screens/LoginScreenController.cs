using System.Collections;
using Kingdom.Boot;
using Kingdom.Contracts;
using Kingdom.Core;
using Kingdom.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kingdom.UI
{
    public sealed class LoginScreenController : MonoBehaviour
    {
        private const string MainSceneName = "Main";

        [SerializeField] private UiTheme theme;
        [SerializeField] private Sprite heroBackdrop;

        private TMP_InputField displayNameInput;
        private TMP_Text statusText;
        private Button playButton;
        private bool submitting;

        public void Configure(Sprite backdrop, UiTheme configuredTheme)
        {
            heroBackdrop = backdrop;
            theme = configuredTheme;
        }

        private void Start()
        {
            BuildView();
        }

        private void BuildView()
        {
            UiTheme resolved = UiTheme.Resolve(theme);
            CanvasView canvas = UiFactory.CreateCanvas("LoginCanvas", transform, resolved);
            ScreenUiBuilder.CreateBackground(
                canvas.Canvas.transform,
                "FrontierMarshal",
                heroBackdrop,
                resolved.Colors.Background);
            UiFactory.CreatePanel(
                canvas.Canvas.transform,
                "ArtScrim",
                new Color(0.02f, 0.05f, 0.07f, 0.46f));
            canvas.SafeArea.SetAsLastSibling();

            RectTransform titleGroup = UiFactory.CreatePanel(
                canvas.SafeArea,
                "TitleGroup",
                Color.clear,
                new Vector2(0.06f, 0.68f),
                new Vector2(0.94f, 0.94f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(
                titleGroup,
                resolved.Spacing.Small,
                new RectOffset(24, 24, 24, 24),
                TextAnchor.UpperLeft);

            TMP_Text eyebrow = UiFactory.CreateText(
                titleGroup,
                "Eyebrow",
                "A NEW FRONTIER AWAITS",
                resolved.Typography.Caption,
                resolved.Colors.GoldBright,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(eyebrow, 48f);

            TMP_Text title = UiFactory.CreateText(
                titleGroup,
                "Title",
                "KINGDOM ONE",
                resolved.Typography.Hero,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(title, 96f);

            TMP_Text subtitle = UiFactory.CreateText(
                titleGroup,
                "Subtitle",
                "Build a luminous citadel. Rally your alliance. Shape the realm.",
                resolved.Typography.Body,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.TopLeft);
            ScreenUiBuilder.SetLayout(subtitle, 120f);

            RectTransform card = UiFactory.CreatePanel(
                canvas.SafeArea,
                "LoginCard",
                resolved.Colors.Overlay,
                new Vector2(0.055f, 0.045f),
                new Vector2(0.945f, 0.39f),
                Vector4.zero);
            ScreenUiBuilder.AddVerticalLayout(
                card,
                resolved.Spacing.Medium,
                new RectOffset(40, 40, 36, 36));

            TMP_Text cardTitle = UiFactory.CreateText(
                card,
                "CardTitle",
                "Enter the realm",
                resolved.Typography.Title,
                resolved.Colors.TextPrimary,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            ScreenUiBuilder.SetLayout(cardTitle, 60f);

            TMP_Text helper = UiFactory.CreateText(
                card,
                "Helper",
                "Choose an original ruler name. Your local guest account can be linked later.",
                resolved.Typography.Caption,
                resolved.Colors.TextSecondary,
                TextAlignmentOptions.TopLeft);
            ScreenUiBuilder.SetLayout(helper, 76f);

            displayNameInput = ScreenUiBuilder.CreateInput(card, "DisplayName", "Ruler name", resolved);
            displayNameInput.text = PlayerPrefs.GetString("kingdom.display-name", "Aster Vale");

            ButtonView play = UiFactory.CreateButton(card, "PlayButton", "BEGIN JOURNEY", resolved);
            playButton = play.Button;
            playButton.onClick.AddListener(BeginGuestLogin);

            statusText = UiFactory.CreateText(
                card,
                "Status",
                "Local Kingdom 1  •  API localhost:3000",
                resolved.Typography.Caption,
                resolved.Colors.TextSecondary,
                TextAlignmentOptions.Center);
            ScreenUiBuilder.SetLayout(statusText, 56f);
        }

        private void BeginGuestLogin()
        {
            if (submitting)
                return;

            string displayName = displayNameInput.text.Trim();
            if (displayName.Length < 3)
            {
                SetStatus("Enter at least three characters.", true);
                return;
            }

            PlayerPrefs.SetString("kingdom.display-name", displayName);
            PlayerPrefs.Save();
            submitting = true;
            playButton.interactable = false;
            SetStatus("Contacting Kingdom 1…", false);

            ApiClient apiClient;
            GameSession session;
            try
            {
                apiClient = Bootstrapper.Resolve<ApiClient>();
                session = Bootstrapper.Resolve<GameSession>();
            }
            catch (System.InvalidOperationException)
            {
                CompleteWithError("Open the Boot scene to start the game services.");
                return;
            }

            StartCoroutine(apiClient.AuthenticateGuest(
                ScreenUiBuilder.InstallationId(),
                displayName,
                authentication => OnAuthenticated(apiClient, session, authentication),
                CompleteWithError));
        }

        private void OnAuthenticated(ApiClient apiClient, GameSession session, GuestAuthData authentication)
        {
            session.ApplyGuestAuthentication(authentication);
            SetStatus("Synchronizing your city…", false);
            StartCoroutine(apiClient.GetBootstrap(
                bootstrap => OnBootstrapLoaded(session, bootstrap),
                CompleteWithError));
        }

        private void OnBootstrapLoaded(GameSession session, BootstrapData bootstrap)
        {
            session.ApplyBootstrap(bootstrap);
            SetStatus("The realm is ready.", false);
            StartCoroutine(OpenMainScene());
        }

        private IEnumerator OpenMainScene()
        {
            yield return new WaitForSecondsRealtime(0.15f);
            if (!Application.CanStreamedLevelBeLoaded(MainSceneName))
            {
                CompleteWithError("Main scene is missing from the active build profile.");
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        }

        private void CompleteWithError(string message)
        {
            submitting = false;
            if (playButton != null)
                playButton.interactable = true;
            SetStatus(message, true);
        }

        private void SetStatus(string message, bool error)
        {
            if (statusText == null)
                return;
            UiTheme resolved = UiTheme.Resolve(theme);
            statusText.text = message;
            statusText.color = error ? resolved.Colors.Danger : resolved.Colors.TextSecondary;
        }
    }
}
