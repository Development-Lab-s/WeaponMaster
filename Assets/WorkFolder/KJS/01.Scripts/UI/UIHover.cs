using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.UI
{
    public class UIHover : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler
    {
        private Image _image;
        [SerializeField]private float duration = 0.3f;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _image.DOFade(1, duration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _image.DOFade(0, duration);
        }
    }
}
