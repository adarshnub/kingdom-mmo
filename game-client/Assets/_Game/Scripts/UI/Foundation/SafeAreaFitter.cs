using UnityEngine;

namespace Kingdom.UI
{
    /// <summary>
    /// Constrains a full-screen RectTransform to the device safe area.
    /// Recalculates only when the safe area or screen dimensions change.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool conformX = true;
        [SerializeField] private bool conformY = true;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize = new Vector2Int(-1, -1);

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            EnsureReference();
            ApplyIfChanged(true);
        }

        private void Update()
        {
            ApplyIfChanged(false);
        }

        private void EnsureReference()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
        }

        private void ApplyIfChanged(bool force)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            if (!conformX)
            {
                safeArea.x = 0f;
                safeArea.width = Screen.width;
            }

            if (!conformY)
            {
                safeArea.y = 0f;
                safeArea.height = Screen.height;
            }

            Vector2 minimum = safeArea.position;
            Vector2 maximum = safeArea.position + safeArea.size;
            minimum.x /= Screen.width;
            minimum.y /= Screen.height;
            maximum.x /= Screen.width;
            maximum.y /= Screen.height;

            rectTransform.anchorMin = minimum;
            rectTransform.anchorMax = maximum;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
