using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ScrollCarousel
{
    public class Carousel : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        [Header("Items")]
        public List<RectTransform> items = new List<RectTransform>();

        [Header("Position")]
        public int startItem = 0;
        public float itemSpacing = 50f;

        [Header("Scale")]
        public float centeredScale = 1f;
        public float nonCenteredScale = 0.7f;
        [SerializeField] private float scaleSmoothSpeed = 5f;

        [Header("Rotation")]
        [SerializeField] public float maxRotationAngle = 10f;
        [SerializeField] private float rotationSmoothSpeed = 5f;

        [Header("Swipe Settings")]
        [SerializeField] private float minSwipeDistance = 10f;
        [SerializeField] private float snapSpeed = 10f;
        [SerializeField] public bool infiniteScroll = false;
        [SerializeField] public float circleRadius = 500f;
        
        [Header("Colors")]
        public bool colorAnimation = false;
        public Color focustedColor = Color.white;
        public Color nonFocustedColor = Color.gray;

        private RectTransform rectTransform;
        private int currentItemIndex = 0;
        private Vector2 startDragPosition;
        private bool isSnapping = false;
        private float currentRotationOffset = 0f;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        private void Start()
        {
            FocusItem(startItem);
            PositionItems(false);
        }

        private void Update()
        {
            if (isSnapping)
            {
                MoveToItem();
            }

            UpdateItemsAppearance();
        }

        private float GetItemSpacing(int index)
        {
            float currentItemScale = (index == currentItemIndex) ? centeredScale : nonCenteredScale;
            float nextItemScale = (index + 1 == currentItemIndex) ? centeredScale : nonCenteredScale;
            
            float currentWidth = items[index].rect.width * currentItemScale;
            float nextWidth = items[index + 1].rect.width * nextItemScale;
            
            return (currentWidth + nextWidth) / 2 + itemSpacing;
        }

        private float GetTotalOffset(int index)
        {
            float offset = 0f;
            int startIdx = Math.Min(index, currentItemIndex);
            int endIdx = Math.Max(index, currentItemIndex);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                offset += GetItemSpacing(i);
            }
            
            return index < currentItemIndex ? -offset : offset;
        }

        private void PositionItems(bool animate = true)
        {
            if (items.Count == 0) return;

            Vector2 centerPoint = rectTransform.rect.center;
            float targetTime = animate ? Time.deltaTime * snapSpeed : 1f;

            for (int i = 0; i < items.Count; i++)
            {
                Vector2 targetPosition;
                if (infiniteScroll)
                {
                    float angle = (360f / items.Count) * (i - currentItemIndex);
                    float radians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector2(
                        centerPoint.x + Mathf.Sin(radians) * circleRadius,
                        centerPoint.y + (1 - Mathf.Cos(radians)) * circleRadius * 0.5f
                    );
                }
                else
                {
                    float offset = GetTotalOffset(i);
                    targetPosition = new Vector2(centerPoint.x + offset, centerPoint.y);
                }

                if (animate)
                {
                    items[i].anchoredPosition = Vector2.Lerp(
                        items[i].anchoredPosition,
                        targetPosition,
                        targetTime
                    );
                }
                else
                {
                    items[i].anchoredPosition = targetPosition;
                }
            }
        }

        private void UpdateItemsAppearance()
        {
            if (items.Count == 0) return;

            Vector2 centerPoint = rectTransform.rect.center;
            float maxDistance = infiniteScroll ? circleRadius : GetItemSpacing(0);

            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i]) continue;

                float distance;
                if (infiniteScroll)
                {
                    float angle = (360f / items.Count) * (i - currentItemIndex);
                    distance = Mathf.Abs(angle) * circleRadius / 180f;
                }
                else
                {
                    distance = Mathf.Abs(items[i].anchoredPosition.x - centerPoint.x);
                }

                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

                // Scale
                float targetScale = Mathf.Lerp(centeredScale, nonCenteredScale, normalizedDistance);
                Vector3 newScale = new Vector3(targetScale, targetScale, 1f);
                if (!float.IsNaN(newScale.x) && !float.IsNaN(newScale.y))
                {
                    items[i].localScale = newScale;
                }

                // Rotation
                float rotationSign = (items[i].anchoredPosition.x > centerPoint.x) ? 1f : -1f;
                float targetRotationY = maxRotationAngle * normalizedDistance * rotationSign;
                if (!float.IsNaN(targetRotationY))
                {
                    items[i].localRotation = Quaternion.Slerp(
                        items[i].localRotation,
                        Quaternion.Euler(30, targetRotationY, 0),
                        Time.deltaTime * rotationSmoothSpeed
                    );
                }
            }
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            isSnapping = false;
            startDragPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (items.Count == 0) return;

            if (infiniteScroll)
            {
                float rotationDelta = (eventData.delta.x / circleRadius) * 45f;
                currentRotationOffset += rotationDelta;
                RotateItemsCircular(currentRotationOffset);
            }
            else
            {
                float leftBound = rectTransform.rect.center.x + GetTotalOffset(0);
                float rightBound = rectTransform.rect.center.x + GetTotalOffset(items.Count - 1);
                
                float currentCenterItemPos = items[currentItemIndex].anchoredPosition.x;
                
                if ((currentCenterItemPos >= leftBound && eventData.delta.x > 0) ||
                    (currentCenterItemPos <= rightBound && eventData.delta.x < 0))
                {
                    float dragFactor = 1f;
                    if (currentCenterItemPos > leftBound || 
                        currentCenterItemPos < rightBound)
                    {
                        dragFactor = 0.5f;
                    }

                    foreach (RectTransform item in items)
                    {
                        item.anchoredPosition += new Vector2(eventData.delta.x * dragFactor, 0);
                    }
                }
            }
        }

        private void RotateItemsCircular(float rotationOffset)
        {
            Vector2 centerPoint = rectTransform.rect.center;
            
            for (int i = 0; i < items.Count; i++)
            {
                float baseAngle = (360f / items.Count) * (i - currentItemIndex);
                float angle = baseAngle + rotationOffset;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector2 targetPosition = new Vector2(
                    centerPoint.x + Mathf.Sin(radians) * circleRadius,
                    centerPoint.y + (1 - Mathf.Cos(radians)) * circleRadius * 0.5f
                );
                
                items[i].anchoredPosition = targetPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float swipeDistance = eventData.position.x - startDragPosition.x;
            
            if (Mathf.Abs(swipeDistance) >= minSwipeDistance)
            {
                if (swipeDistance > 0)
                {
                    if (infiniteScroll || currentItemIndex > 0)
                    {
                        GoToPrevious();
                    }
                }
                else if (swipeDistance < 0)
                {
                    if (infiniteScroll || currentItemIndex < items.Count - 1)
                    {
                        GoToNext();
                    }
                }
            }
            
            currentRotationOffset = 0f;
            FocusItem(currentItemIndex);
            startDragPosition = Vector2.zero;
        }

        private void MoveToItem()
        {
            PositionItems(true);
            
            // Check if we're close enough to stop snapping
            RectTransform targetItem = items[currentItemIndex];
            if (Mathf.Abs(targetItem.anchoredPosition.x - rectTransform.rect.center.x) < 0.1f)
            {
                isSnapping = false;
                PositionItems(false);
            }
        }

        public void FocusItem(RectTransform item)
        {
            FocusItem(items.IndexOf(item));
        }

        private void FocusItem(int index)
        {
            if (index < 0 || index >= items.Count) return;

            currentItemIndex = index;
            currentRotationOffset = 0f;
            isSnapping = true;
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bool isFocused = i == currentItemIndex;
                
                item.GetComponent<CarouselButton>()?.SetFocus(isFocused);
                
                if (colorAnimation)
                {
                    var image = item.GetComponent<Image>();
                    if (image != null)
                    {
                        Color targetColor = isFocused ? focustedColor : nonFocustedColor;
                        StartCoroutine(ColorAnimation(item, targetColor));
                    }
                }
            }
        }

        private void GoToNext()
        {
            if (infiniteScroll)
            {
                FocusItem((currentItemIndex + 1) % items.Count);
            }
            else if (currentItemIndex < items.Count - 1)
            {
                FocusItem(currentItemIndex + 1);
            }
        }

        private void GoToPrevious()
        {
            if (infiniteScroll)
            {
                FocusItem((currentItemIndex - 1 + items.Count) % items.Count);
            }
            else if (currentItemIndex > 0)
            {
                FocusItem(currentItemIndex - 1);
            }
        }

        private IEnumerator ColorAnimation(RectTransform item, Color targetColor)
        {
            Image image = item.GetComponent<Image>();
            if (image == null) yield break;

            Color startColor = image.color;
            float elapsedTime = 0f;
            float duration = 0.2f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                image.color = Color.Lerp(startColor, targetColor, elapsedTime / duration);
                yield return null;
            }
        }

        public void ForceUpdate()
        {
            PositionItems(false);
            UpdateItemsAppearance();
        }
    }
}
