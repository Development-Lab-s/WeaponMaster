using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public class CustomizingMove : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private GameObject tab;
        private Sequence seq;
        private void Start()
        {
            tab.SetActive(false);
        }

        public void ShowCustomizing()
        {
            seq?.Kill();
            seq = DOTween.Sequence();
            seq.AppendCallback(() => tab.SetActive(true));
            seq.Append(background.DOFade(0f, 1f));
        }

        public void HideCustomizing()
        {
            seq?.Kill();
            seq = DOTween.Sequence();
            seq.Append(background.DOFade(1f, 0.1f));
            seq.AppendCallback(() => tab.SetActive(false));
        }
    }
}