using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ScrollCarousel.Demo
{
    public class ButtonClicker : MonoBehaviour
    {
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Carousel _carousel;

        public void ChangeButtonClicked(int num)
        {
            _subtitle.text = $"Clicked Button: {num}";
        }

        public void OpenSourceRepository()
        {
            Application.OpenURL("https://github.com/pablogozalvez/ScrollCarousel");
        }

        public void OpenAssetStorePage()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/gui/scroll-carousel-306533");
        }

        public void ToggleCarouselMode()
        {
            _carousel.InfiniteScroll = _toggle.isOn;
            _carousel.ForceUpdate();
        }
    }
}