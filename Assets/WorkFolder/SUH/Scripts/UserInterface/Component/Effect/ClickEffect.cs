using DG.Tweening;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.UserInterface.Component.Effect
{
    public class ClickEffect : MonoBehaviour
    {
        [SerializeField] private Vector3 scaleVector3;
        [SerializeField] private float duration;
        [SerializeField] private int swing;
        [SerializeField] private float elasticity;

        private bool _debounce = true;
        
        public void OnMouseClick()
        {
            if (!_debounce)
                return;
            _debounce = false;
            Sequence s = DOTween.Sequence();
            s.Append(transform.DOPunchScale(scaleVector3, duration, swing, elasticity))
                .AppendCallback(() =>
                {
                    _debounce = true;
                });
        }
    }
}