using System;
using TMPro;
using UnityEngine;

namespace Kingdom.UI
{
    /// <summary>
    /// Semantic visual tokens for the game's portrait mobile interface.
    /// Screens should consume these roles instead of embedding raw values.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Kingdom/UI Theme")]
    public sealed class UiTheme : ScriptableObject
    {
        [SerializeField] private UiColors colors = new UiColors();
        [SerializeField] private UiSpacing spacing = new UiSpacing();
        [SerializeField] private UiTypography typography = new UiTypography();
        [SerializeField] private UiMotion motion = new UiMotion();

        private static UiTheme defaultTheme;

        public UiColors Colors => colors;
        public UiSpacing Spacing => spacing;
        public UiTypography Typography => typography;
        public UiMotion Motion => motion;

        /// <summary>
        /// A dependency-free fallback suitable for generated scenes and prototypes.
        /// A project theme asset can replace it without changing screen code.
        /// </summary>
        public static UiTheme Default
        {
            get
            {
                if (defaultTheme != null)
                {
                    return defaultTheme;
                }

                defaultTheme = CreateInstance<UiTheme>();
                defaultTheme.name = "Runtime Default UI Theme";
                defaultTheme.hideFlags = HideFlags.HideAndDontSave;
                return defaultTheme;
            }
        }

        public static UiTheme Resolve(UiTheme theme)
        {
            return theme != null ? theme : Default;
        }
    }

    [Serializable]
    public sealed class UiColors
    {
        [Header("Surfaces")]
        [SerializeField] private Color background = new Color32(15, 19, 21, 255);
        [SerializeField] private Color surface = new Color32(30, 36, 40, 255);
        [SerializeField] private Color surfaceRaised = new Color32(43, 49, 53, 255);
        [SerializeField] private Color overlay = new Color32(8, 10, 11, 220);
        [SerializeField] private Color parchment = new Color32(222, 207, 165, 255);
        [SerializeField] private Color parchmentDark = new Color32(173, 151, 103, 255);

        [Header("Accents")]
        [SerializeField] private Color gold = new Color32(216, 170, 76, 255);
        [SerializeField] private Color goldBright = new Color32(246, 209, 121, 255);
        [SerializeField] private Color royalBlue = new Color32(48, 97, 132, 255);
        [SerializeField] private Color success = new Color32(92, 164, 91, 255);
        [SerializeField] private Color danger = new Color32(155, 61, 52, 255);
        [SerializeField] private Color warning = new Color32(213, 137, 53, 255);

        [Header("Content")]
        [SerializeField] private Color textPrimary = new Color32(246, 240, 221, 255);
        [SerializeField] private Color textSecondary = new Color32(188, 187, 178, 255);
        [SerializeField] private Color textOnLight = new Color32(37, 31, 24, 255);
        [SerializeField] private Color divider = new Color32(101, 91, 70, 255);
        [SerializeField] private Color disabled = new Color32(103, 106, 104, 190);

        public Color Background => background;
        public Color Surface => surface;
        public Color SurfaceRaised => surfaceRaised;
        public Color Overlay => overlay;
        public Color Parchment => parchment;
        public Color ParchmentDark => parchmentDark;
        public Color Gold => gold;
        public Color GoldBright => goldBright;
        public Color RoyalBlue => royalBlue;
        public Color Success => success;
        public Color Danger => danger;
        public Color Warning => warning;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color TextOnLight => textOnLight;
        public Color Divider => divider;
        public Color Disabled => disabled;
    }

    [Serializable]
    public sealed class UiSpacing
    {
        [Tooltip("Reference resolution used by generated portrait canvases.")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private float hairline = 2f;
        [SerializeField] private float tiny = 8f;
        [SerializeField] private float small = 16f;
        [SerializeField] private float medium = 24f;
        [SerializeField] private float large = 32f;
        [SerializeField] private float extraLarge = 48f;
        [Tooltip("96 design pixels maps to at least 48dp at the target 2x density.")]
        [SerializeField] private float minimumTouchTarget = 96f;
        [SerializeField, Range(0f, 1f)] private float canvasWidthHeightMatch = 0.5f;

        public Vector2 ReferenceResolution => referenceResolution;
        public float Hairline => hairline;
        public float Tiny => tiny;
        public float Small => small;
        public float Medium => medium;
        public float Large => large;
        public float ExtraLarge => extraLarge;
        public float MinimumTouchTarget => minimumTouchTarget;
        public float CanvasWidthHeightMatch => canvasWidthHeightMatch;
    }

    [Serializable]
    public sealed class UiTypography
    {
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset displayFont;
        [SerializeField] private float hero = 72f;
        [SerializeField] private float heading = 48f;
        [SerializeField] private float title = 40f;
        [SerializeField] private float body = 32f;
        [SerializeField] private float caption = 26f;
        [SerializeField] private float button = 32f;

        public TMP_FontAsset RegularFont => regularFont;
        public TMP_FontAsset DisplayFont => displayFont != null ? displayFont : regularFont;
        public float Hero => hero;
        public float Heading => heading;
        public float Title => title;
        public float Body => body;
        public float Caption => caption;
        public float Button => button;
    }

    [Serializable]
    public sealed class UiMotion
    {
        [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.96f;
        [SerializeField, Min(1f)] private float pressResponse = 22f;
        [SerializeField, Range(0.05f, 0.5f)] private float submitHoldSeconds = 0.12f;

        public float PressedScale => pressedScale;
        public float PressResponse => pressResponse;
        public float SubmitHoldSeconds => submitHoldSeconds;
    }
}
