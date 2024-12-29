using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ButtonClicker : MonoBehaviour
{
    [SerializeField] private TMP_Text _subtitle;

    public void ChangeButtonClicked(int num)
    {
        _subtitle.text = $"Clicked Button: {num}";
    }

    public void OpenSourceRepository()
    {
        Application.OpenURL("https://github.com/pablogozalvez/ScrollCarousel");
    }
}
