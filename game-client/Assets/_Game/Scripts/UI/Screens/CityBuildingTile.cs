using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kingdom.UI
{
    [DisallowMultipleComponent]
    public sealed class CityBuildingTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private RectTransform mapRoot;
        private Vector2[] snapPoints;
        private Action<Vector2> dragFinished;
        private bool dragging;

        public void Configure(
            RectTransform configuredMapRoot,
            Vector2[] configuredSnapPoints,
            Vector2 startPosition,
            Action<Vector2> onDragFinished)
        {
            rectTransform = (RectTransform)transform;
            mapRoot = configuredMapRoot;
            snapPoints = configuredSnapPoints ?? Array.Empty<Vector2>();
            dragFinished = onDragFinished;
            SetNormalizedPosition(startPosition);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = mapRoot != null && eventData.button == PointerEventData.InputButton.Left;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || mapRoot == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mapRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Rect rect = mapRoot.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            Vector2 normalized = new(
                Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width),
                Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height));
            SetNormalizedPosition(normalized);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            dragging = false;
            Vector2 snapped = FindClosestSnapPoint(rectTransform.anchorMin);
            SetNormalizedPosition(snapped);
            dragFinished?.Invoke(snapped);
        }

        private Vector2 FindClosestSnapPoint(Vector2 position)
        {
            if (snapPoints.Length == 0)
                return position;

            Vector2 closest = snapPoints[0];
            float closestDistance = Vector2.SqrMagnitude(position - closest);
            for (int index = 1; index < snapPoints.Length; index++)
            {
                float distance = Vector2.SqrMagnitude(position - snapPoints[index]);
                if (distance >= closestDistance)
                    continue;

                closest = snapPoints[index];
                closestDistance = distance;
            }

            return closest;
        }

        private void SetNormalizedPosition(Vector2 position)
        {
            if (rectTransform == null)
                rectTransform = (RectTransform)transform;

            rectTransform.anchorMin = position;
            rectTransform.anchorMax = position;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
