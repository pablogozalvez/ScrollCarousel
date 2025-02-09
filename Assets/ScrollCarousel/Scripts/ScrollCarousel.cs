using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.ComponentModel;

namespace ScrollCarousel
{
    public class Carousel : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        [Header("Items")]
        [Description("List of items to be displayed in the carousel")]
        public List<RectTransform> Items = new List<RectTransform>();

        [Header("Position")]
        [Description("Index of the item that will be centered at the start")]
        public int StartItem = 0;
        [Description("Spacing between items")]
        public float Itemspacing = 50f;

        [Header("Scale")]
        [Description("Scale of the centered item")]
        public float CenteredScale = 1f;
        [Description("Scale of the non-centered items")]
        public float NonCenteredScale = 0.7f;
        [Header("Rotation")]
        [Description("Maximum rotation angle of the items")]
        [SerializeField] public float MaxRotationAngle = 10f;
        [SerializeField] private float _rotationSmoothSpeed = 5f;

        [Header("Swipe Settings")]
        [SerializeField] private float _snapSpeed = 10f;
        [Description("Enable infinite scrolling")]
        [SerializeField] public bool InfiniteScroll = false;
        [Description("Radius of the infinite scroll circle")]
        [SerializeField] public float CircleRadius = 500f;
        
        [Header("Colors")]
        [Description("Enable color animation")]
        public bool ColorAnimation = false;
        [Description("Color of the focused item")]
        public Color FocustedColor = Color.white;
        [Description("Color of the non-focused items")]
        public Color NonFocustedColor = Color.gray;

        private RectTransform _rectTransform;
        private int _currentItemIndex = 0;
        private Vector2 _startDragPosition;
        private bool _isSnapping = false;
        private float _currentRotationOffset = 0f;
        private Dictionary<RectTransform, Coroutine> _activeColorAnimations = new Dictionary<RectTransform, Coroutine>();

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        private void Start()
        {
            FocusItem(StartItem);
            ForceUpdate();
        }

        private void Update()
        {
            if (_isSnapping)
            {
                MoveToItem();
            }

            UpdateItemsAppearance();
        }

        private float GetItemspacing(int index)
        {
            float currentItemscale = (index == _currentItemIndex) ? CenteredScale : NonCenteredScale;
            float nextItemscale = (index + 1 == _currentItemIndex) ? CenteredScale : NonCenteredScale;
            
            float currentWidth = Items[index].rect.width * currentItemscale;
            float nextWidth = Items[index + 1].rect.width * nextItemscale;
            
            return (currentWidth + nextWidth) / 2 + Itemspacing;
        }

        private float GetTotalOffset(int index)
        {
            float offset = 0f;
            int startIdx = Math.Min(index, _currentItemIndex);
            int endIdx = Math.Max(index, _currentItemIndex);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                offset += GetItemspacing(i);
            }
            
            return index < _currentItemIndex ? -offset : offset;
        }

        private void PositionItems(bool animate = true)
        {
            if (Items.Count == 0) return;

            Vector2 centerPoint = _rectTransform.rect.center;
            float targetTime = animate ? Time.deltaTime * _snapSpeed : 1f;

            for (int i = 0; i < Items.Count; i++)
            {
                Vector2 targetPosition;
                if (InfiniteScroll)
                {
                    float angle = (360f / Items.Count) * (i - _currentItemIndex);
                    float radians = angle * Mathf.Deg2Rad;
                    targetPosition = new Vector2(
                        centerPoint.x + Mathf.Sin(radians) * CircleRadius,
                        centerPoint.y + (1 - Mathf.Cos(radians)) * CircleRadius * 0.5f
                    );
                }
                else
                {
                    float offset = GetTotalOffset(i);
                    targetPosition = new Vector2(centerPoint.x + offset, centerPoint.y);
                }

                if (animate)
                {
                    Items[i].anchoredPosition = Vector2.Lerp(
                        Items[i].anchoredPosition,
                        targetPosition,
                        targetTime
                    );
                }
                else
                {
                    Items[i].anchoredPosition = targetPosition;
                }
            }
        }

        private void UpdateItemsAppearance()
        {
            if (Items.Count == 0) return;

            Vector2 centerPoint = _rectTransform.rect.center;
            float maxDistance = InfiniteScroll ? CircleRadius : GetItemspacing(0);
            float minDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i]) continue;

                float distance;
                float angleDistance;
                if (InfiniteScroll)
                {
                    float angle = (360f / Items.Count) * (i - _currentItemIndex) + _currentRotationOffset;
                    distance = Mathf.Abs(Mathf.DeltaAngle(0, angle)) / (360f / Items.Count) * CircleRadius;
                    angleDistance = Mathf.Abs(Mathf.DeltaAngle(0, angle)) / (360f / Items.Count);
                }
                else
                {
                    distance = Mathf.Abs(Items[i].anchoredPosition.x - centerPoint.x);
                    angleDistance = Mathf.Abs(i - _currentItemIndex);
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }

                Items[i].SetSiblingIndex(Items.Count - (int)(angleDistance * 2));

                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

                // Scale
                float targetScale = Mathf.Lerp(CenteredScale, NonCenteredScale, normalizedDistance);
                Vector3 newScale = new Vector3(targetScale, targetScale, 1f);
                if (!float.IsNaN(newScale.x) && !float.IsNaN(newScale.y))
                {
                    Items[i].localScale = newScale;
                }

                // Rotation
                float rotationSign = (Items[i].anchoredPosition.x > centerPoint.x) ? 1f : -1f;
                float targetRotationY = MaxRotationAngle * normalizedDistance * rotationSign;
                if (!float.IsNaN(targetRotationY))
                {
                    Items[i].localRotation = Quaternion.Slerp(
                        Items[i].localRotation,
                        Quaternion.Euler(30, targetRotationY, 0),
                        Time.deltaTime * _rotationSmoothSpeed
                    );
                }
            }

            if (ColorAnimation)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    Color targetColor = (i == closestIndex) ? FocustedColor : NonFocustedColor;
                    StartColorAnimation(Items[i], targetColor);
                }
            }
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isSnapping = false;
            _startDragPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Items.Count == 0) return;

            if (InfiniteScroll)
            {
                float rotationDelta = (eventData.delta.x / CircleRadius) * 45f;
                _currentRotationOffset += rotationDelta;
                RotateItemsCircular(_currentRotationOffset);
            }
            else
            {
                float leftBound = _rectTransform.rect.center.x + GetTotalOffset(0);
                float rightBound = _rectTransform.rect.center.x + GetTotalOffset(Items.Count - 1);
                
                float currentCenterItemPos = Items[_currentItemIndex].anchoredPosition.x;
                
                if ((currentCenterItemPos >= leftBound && eventData.delta.x > 0) ||
                    (currentCenterItemPos <= rightBound && eventData.delta.x < 0))
                {
                    float dragFactor = 1f;
                    if (currentCenterItemPos > leftBound || 
                        currentCenterItemPos < rightBound)
                    {
                        dragFactor = 0.5f;
                    }

                    foreach (RectTransform item in Items)
                    {
                        item.anchoredPosition += new Vector2(eventData.delta.x * dragFactor, 0);
                    }
                }
            }
        }

        private void RotateItemsCircular(float rotationOffset)
        {
            Vector2 centerPoint = _rectTransform.rect.center;
            
            for (int i = 0; i < Items.Count; i++)
            {
                float baseAngle = (360f / Items.Count) * (i - _currentItemIndex);
                float angle = baseAngle + rotationOffset;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector2 targetPosition = new Vector2(
                    centerPoint.x + Mathf.Sin(radians) * CircleRadius,
                    centerPoint.y + (1 - Mathf.Cos(radians)) * CircleRadius * 0.5f
                );
                
                Items[i].anchoredPosition = targetPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (InfiniteScroll)
            {
                float closestDistance = float.MaxValue;
                int closestIndex = 0;
                for (int i = 0; i < Items.Count; i++)
                {
                    float angle = (360f / Items.Count) * (i - _currentItemIndex) + _currentRotationOffset;
                    float distance = Mathf.Abs(Mathf.DeltaAngle(0, angle));
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }
                FocusItem(closestIndex);
            }
            else
            {
                float centerX = _rectTransform.rect.center.x;
                int closestIndex = 0;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < Items.Count; i++)
                {
                    float distance = Mathf.Abs(Items[i].anchoredPosition.x - centerX);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }
                FocusItem(closestIndex);
            }

            _currentRotationOffset = 0f;
            _startDragPosition = Vector2.zero;
        }

        private void MoveToItem()
        {
            PositionItems(true);
            
            // Check if we're close enough to stop snapping
            RectTransform targetItem = Items[_currentItemIndex];
            if (Mathf.Abs(targetItem.anchoredPosition.x - _rectTransform.rect.center.x) < 0.1f)
            {
                _isSnapping = false;
                PositionItems(false);
            }
        }

        public void FocusItem(RectTransform item)
        {
            FocusItem(Items.IndexOf(item));
        }

        private void FocusItem(int index)
        {
            if (index < 0 || index >= Items.Count) return;

            _currentItemIndex = index;
            _currentRotationOffset = 0f;
            _isSnapping = true;
            
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                bool isFocused = i == _currentItemIndex;
                
                item.GetComponent<CarouselButton>()?.SetFocus(isFocused);
            }
        }

        public void GoToNext()
        {
            if (InfiniteScroll)
            {
                FocusItem((_currentItemIndex + 1) % Items.Count);
            }
            else if (_currentItemIndex < Items.Count - 1)
            {
                FocusItem(_currentItemIndex + 1);
            }
        }

        public void GoToPrevious()
        {
            if (InfiniteScroll)
            {
                FocusItem((_currentItemIndex - 1 + Items.Count) % Items.Count);
            }
            else if (_currentItemIndex > 0)
            {
                FocusItem(_currentItemIndex - 1);
            }
        }

        public void ForceUpdate()
        {
            PositionItems(false);
            UpdateItemsAppearance();
        }

        private void StartColorAnimation(RectTransform item, Color targetColor)
        {
            if (_activeColorAnimations.ContainsKey(item))
            {
                StopCoroutine(_activeColorAnimations[item]);
                _activeColorAnimations.Remove(item);
            }
            _activeColorAnimations[item] = StartCoroutine(ColorAnimationCoroutine(item, targetColor));
        }

        private IEnumerator ColorAnimationCoroutine(RectTransform item, Color targetColor)
        {
            Image image = item.GetComponent<Image>();
            if (image == null) 
            {
                _activeColorAnimations.Remove(item);
                yield break;
            }

            Color startColor = image.color;
            float elapsedTime = 0f;
            float duration = 0.2f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                image.color = Color.Lerp(startColor, targetColor, elapsedTime / duration);
                yield return null;
            }

            image.color = targetColor;
            _activeColorAnimations.Remove(item);
        }
    }
}
