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
            serializedObject.Update();

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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Items"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("StartItem"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Itemspacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CenteredScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NonCenteredScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxRotationAngle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationSmoothSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_snapSpeed"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("InfiniteScroll"));
            if (serializedObject.FindProperty("InfiniteScroll").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CircleRadius"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ColorAnimation"));
            if (serializedObject.FindProperty("ColorAnimation").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FocustedColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("NonFocustedColor"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddAllChildrenToItemList()
        {
            carousel.Items.Clear();
            foreach (Transform child in carousel.transform)
            {
                if (child is RectTransform rectTransform)
                {
                    carousel.Items.Add(rectTransform);
                }
            }
            EditorUtility.SetDirty(carousel);
        }

        private void OrganizeItemsInEditor()
        {
            if (carousel.Items.Count == 0) return;

            Vector2 centerPoint = carousel.GetComponent<RectTransform>().rect.center;

            for (int i = 0; i < carousel.Items.Count; i++)
            {
                Vector2 targetPosition;
                if (carousel.InfiniteScroll)
                {
                    float angle = (360f / carousel.Items.Count) * (i - carousel.StartItem);
                    float radians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector2(
                        centerPoint.x + Mathf.Sin(radians) * carousel.CircleRadius,
                        centerPoint.y + (1 - Mathf.Cos(radians)) * carousel.CircleRadius * 0.5f
                    );
                }
                else
                {
                    float offset = GetTotalOffset(i) - GetTotalOffset(carousel.StartItem);
                    targetPosition = new Vector2(centerPoint.x + offset, centerPoint.y);
                }

                carousel.Items[i].anchoredPosition = targetPosition;
            }

            EditorUtility.SetDirty(carousel);
        }

        private void UpdateItemsAppearanceInEditor()
        {
            if (carousel.Items.Count == 0) return;

            Vector2 centerPoint = carousel.GetComponent<RectTransform>().rect.center;
            float maxDistance = carousel.InfiniteScroll ? carousel.CircleRadius : GetItemspacing(0);

            for (int i = 0; i < carousel.Items.Count; i++)
            {
                if (!carousel.Items[i]) continue;

                // Update visual sorting order based on distance from center
                float visualDistance = Mathf.Abs(i - carousel.StartItem);
                carousel.Items[i].SetSiblingIndex(carousel.Items.Count - (int)(visualDistance * 2));

                float distance;
                if (carousel.InfiniteScroll)
                {
                    float angle = (360f / carousel.Items.Count) * (i - carousel.StartItem);
                    distance = Mathf.Abs(angle) * carousel.CircleRadius / 180f;
                }
                else
                {
                    distance = Mathf.Abs(carousel.Items[i].anchoredPosition.x - centerPoint.x);
                }

                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

                // Scale
                float targetScale = i == carousel.StartItem ? carousel.CenteredScale : carousel.NonCenteredScale;
                Vector3 newScale = new Vector3(targetScale, targetScale, 1f);
                if (!float.IsNaN(newScale.x) && !float.IsNaN(newScale.y))
                {
                    carousel.Items[i].localScale = newScale;
                }

                // Rotation
                float rotationSign = (carousel.Items[i].anchoredPosition.x > centerPoint.x) ? 1f : -1f;
                float targetRotationY = carousel.MaxRotationAngle * normalizedDistance * rotationSign;
                if (!float.IsNaN(targetRotationY))
                {
                    carousel.Items[i].localRotation = Quaternion.Euler(30, targetRotationY, 0);
                }
            }

            EditorUtility.SetDirty(carousel);
        }

        private float GetItemspacing(int index)
        {
            float currentItemscale = (index == carousel.StartItem) ? carousel.CenteredScale : carousel.NonCenteredScale;
            float nextItemscale = (index + 1 == carousel.StartItem) ? carousel.CenteredScale : carousel.NonCenteredScale;
            
            float currentWidth = carousel.Items[index].rect.width * currentItemscale;
            float nextWidth = carousel.Items[index + 1].rect.width * nextItemscale;
            
            return (currentWidth + nextWidth) / 2 + carousel.Itemspacing;
        }

        private float GetTotalOffset(int index)
        {
            float offset = 0f;
            int startIdx = Math.Min(index, carousel.StartItem);
            int endIdx = Math.Max(index, carousel.StartItem);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                offset += GetItemspacing(i);
            }
            
            return index < carousel.StartItem ? -offset : offset;
        }
    }
}