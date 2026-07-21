using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kingdom.UI
{
    /// <summary>
    /// Allocation-free scale feedback for pointer and keyboard/controller submits.
    /// The Button component remains responsible for interactability and color states.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class PressFeedback : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        ISubmitHandler
    {
        [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.96f;
        [SerializeField, Min(1f)] private float response = 22f;
        [SerializeField, Range(0.05f, 0.5f)] private float submitHoldSeconds = 0.12f;

        private RectTransform rectTransform;
        private Selectable selectable;
        private Vector3 restingScale;
        private bool pressed;
        private float submitReleaseAt = -1f;

        public void Configure(UiMotion motion)
        {
            if (motion == null)
            {
                return;
            }

            pressedScale = motion.PressedScale;
            response = motion.PressResponse;
            submitHoldSeconds = motion.SubmitHoldSeconds;
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            selectable = GetComponent<Selectable>();
            restingScale = rectTransform.localScale;
        }

        private void OnEnable()
        {
            EnsureReferences();
            pressed = false;
            submitReleaseAt = -1f;
            rectTransform.localScale = restingScale;
        }

        private void Update()
        {
            if (submitReleaseAt >= 0f && Time.unscaledTime >= submitReleaseAt)
            {
                pressed = false;
                submitReleaseAt = -1f;
            }

            float multiplier = pressed && selectable != null && selectable.IsInteractable()
                ? pressedScale
                : 1f;
            Vector3 target = restingScale * multiplier;
            float interpolation = 1f - Mathf.Exp(-response * Time.unscaledDeltaTime);
            rectTransform.localScale = Vector3.LerpUnclamped(rectTransform.localScale, target, interpolation);
        }

        private void OnDisable()
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = restingScale;
            }

            pressed = false;
            submitReleaseAt = -1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
            {
                pressed = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                pressed = false;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            pressed = true;
            submitReleaseAt = Time.unscaledTime + submitHoldSeconds;
        }

        private bool IsInteractable()
        {
            return selectable != null && selectable.IsActive() && selectable.IsInteractable();
        }

        private void EnsureReferences()
        {
            if (rectTransform == null)
            {
                rectTransform = (RectTransform)transform;
                restingScale = rectTransform.localScale;
            }

            if (selectable == null)
            {
                selectable = GetComponent<Selectable>();
            }
        }
    }
}
