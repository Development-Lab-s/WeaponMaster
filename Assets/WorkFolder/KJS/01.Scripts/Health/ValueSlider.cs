using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.Health
{
    public class ValueSlider : MonoBehaviour
    {
        private Slider _slider;
        [Header("Animation")]
        [SerializeField] private float _delay = 0.15f;
        [SerializeField] private float _duration = 0.5f;
        
        private Tween _damageTween;

        private void Awake()
        {
            
            _slider = GetComponent<Slider>();
        }

    }
    
    
}