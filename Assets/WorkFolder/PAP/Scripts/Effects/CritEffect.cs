using System;
using DG.Tweening;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Effects
{
    public class CritEffect : MonoBehaviour
    {
        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _renderer.material.color = new Color(1, 1, 1);
        }

        public void Play(float size)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(_renderer.material.DOColor(Color.yellow * 3.0f,"_BaseColor", 0.2f));
            seq.Join(transform.DOShakeScale(0.2f,size));
            seq.Append(transform.DOScale(Vector3.zero, 0.1f));
            seq.AppendCallback(() => Destroy(gameObject));
        }
    }
}