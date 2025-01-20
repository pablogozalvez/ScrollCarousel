using System;
using UnityEngine.UI;
using UnityEngine;

namespace ScrollCarousel
{
    public class CarouselButton : Button
    {
        [SerializeField] private Action buttonAction;
        private Carousel carousel;
        private bool isFocused = false;

        protected override void Start()
        {
            base.Start();
            carousel = GetComponentInParent<Carousel>();

            if (carousel == null)
            {
                Debug.LogWarning("CarouselButton: No ScrollCarousel found in parent.");
            }
        }

        public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (isFocused)
            {
                base.OnPointerClick(eventData);
                buttonAction?.Invoke();
            }
            else 
            {
                carousel.FocusItem(this.GetComponent<RectTransform>());
            }
        }

        public void SetFocus(bool focus)
        {
            this.isFocused = focus;
        }
    }
}
