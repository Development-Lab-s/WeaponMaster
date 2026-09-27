using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.UI
{
    public class UIHoverText : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler
    {
        [SerializeField]private TextMeshProUGUI text;
        [SerializeField]private float duration;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            text.DOColor(Color.cornflowerBlue,duration);
            text.rectTransform.DOScale(new Vector3(1.05f,1.05f,1.05f),duration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            text.DOColor(Color.white,duration);
            text.rectTransform.DOScale(new Vector3(1f,1f,1f),duration);
            
        }
    }
}
