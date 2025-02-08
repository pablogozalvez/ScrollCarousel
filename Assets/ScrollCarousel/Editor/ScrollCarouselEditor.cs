using System;
using UnityEditor;
using UnityEngine;

namespace ScrollCarousel
{
    [CustomEditor(typeof(Carousel))]
    public class ScrollCarouselEditor : Editor
    {
        private Carousel carousel;
        
        public override void OnInspectorGUI()
        {
            carousel = (Carousel)target;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Carousel Editor", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add All Children to Items"))
            {
                AddAllChildrenToItemList();
            }

            if (GUILayout.Button("Organize Items"))
            {
                OrganizeItemsInEditor();
                UpdateItemsAppearanceInEditor();
                OrganizeItemsInEditor();
            }

            GUILayout.EndVertical();

            base.OnInspectorGUI();
        }

        private void AddAllChildrenToItemList()
        {
            carousel.items.Clear();
            foreach (Transform child in carousel.transform)
            {
                if (child is RectTransform rectTransform)
                {
                    carousel.items.Add(rectTransform);
                }
            }
            EditorUtility.SetDirty(carousel);
        }

        private void OrganizeItemsInEditor()
        {
            if (carousel.items.Count == 0) return;

            Vector2 centerPoint = carousel.GetComponent<RectTransform>().rect.center;

            for (int i = 0; i < carousel.items.Count; i++)
            {
                Vector2 targetPosition;
                if (carousel.infiniteScroll)
                {
                    float angle = (360f / carousel.items.Count) * (i - carousel.startItem);
                    float radians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector2(
                        centerPoint.x + Mathf.Sin(radians) * carousel.circleRadius,
                        centerPoint.y + (1 - Mathf.Cos(radians)) * carousel.circleRadius * 0.5f
                    );
                }
                else
                {
                    float offset = GetTotalOffset(i) - GetTotalOffset(carousel.startItem);
                    targetPosition = new Vector2(centerPoint.x + offset, centerPoint.y);
                }

                carousel.items[i].anchoredPosition = targetPosition;
            }

            EditorUtility.SetDirty(carousel);
        }

        private void UpdateItemsAppearanceInEditor()
        {
            if (carousel.items.Count == 0) return;

            Vector2 centerPoint = carousel.GetComponent<RectTransform>().rect.center;
            float maxDistance = carousel.infiniteScroll ? carousel.circleRadius : GetItemSpacing(0);

            for (int i = 0; i < carousel.items.Count; i++)
            {
                if (!carousel.items[i]) continue;

                float distance;
                if (carousel.infiniteScroll)
                {
                    float angle = (360f / carousel.items.Count) * (i - carousel.startItem);
                    distance = Mathf.Abs(angle) * carousel.circleRadius / 180f;
                }
                else
                {
                    distance = Mathf.Abs(carousel.items[i].anchoredPosition.x - centerPoint.x);
                }

                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

                // Scale
                float targetScale = i == carousel.startItem ? carousel.centeredScale : carousel.nonCenteredScale;
                Vector3 newScale = new Vector3(targetScale, targetScale, 1f);
                if (!float.IsNaN(newScale.x) && !float.IsNaN(newScale.y))
                {
                    carousel.items[i].localScale = newScale;
                }

                // Rotation
                float rotationSign = (carousel.items[i].anchoredPosition.x > centerPoint.x) ? 1f : -1f;
                float targetRotationY = carousel.maxRotationAngle * normalizedDistance * rotationSign;
                if (!float.IsNaN(targetRotationY))
                {
                    carousel.items[i].localRotation = Quaternion.Euler(30, targetRotationY, 0);
                }
            }

            EditorUtility.SetDirty(carousel);
        }

        private float GetItemSpacing(int index)
        {
            float currentItemScale = (index == carousel.startItem) ? carousel.centeredScale : carousel.nonCenteredScale;
            float nextItemScale = (index + 1 == carousel.startItem) ? carousel.centeredScale : carousel.nonCenteredScale;
            
            float currentWidth = carousel.items[index].rect.width * currentItemScale;
            float nextWidth = carousel.items[index + 1].rect.width * nextItemScale;
            
            return (currentWidth + nextWidth) / 2 + carousel.itemSpacing;
        }

        private float GetTotalOffset(int index)
        {
            float offset = 0f;
            int startIdx = Math.Min(index, carousel.startItem);
            int endIdx = Math.Max(index, carousel.startItem);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                offset += GetItemSpacing(i);
            }
            
            return index < carousel.startItem ? -offset : offset;
        }
    }
}