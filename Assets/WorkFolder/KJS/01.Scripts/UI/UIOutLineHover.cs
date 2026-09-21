using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.UI
{
    public class UIOutLineHover : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler
    {
        [SerializeField]private Outline outline;
        [SerializeField]private TextMeshProUGUI text;
        [SerializeField]private float duration;
        public void OnPointerEnter(PointerEventData eventData)
        {
            outline.DOFade(1f, duration);
            text.color = Color.yellow;
            
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            outline.DOFade(0f, duration);
            text.color = Color.white;
        }
    }
}
