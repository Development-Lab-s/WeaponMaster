using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.Health
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _damageSlider;

        [Header("Animation")]
        [SerializeField] private float _delay = 0.15f;
        [SerializeField] private float _duration = 0.5f;

        private Tween _damageTween;

        public static HealthBar Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }


        private void Start()
        {
            _healthSlider.value = 1;
            _damageSlider.value = 1;
        }

        public void ChangeBar(int currentHealth, int maxHealth)
        {
            float health = (float)currentHealth/maxHealth;
            _healthSlider.value = health;
            _damageTween?.Kill();
            _damageTween = _damageSlider.DOValue(health, _duration).SetDelay(_delay).SetEase(Ease.OutQuad);
        }

        private void OnDisable()
        {
            _damageTween?.Kill();
        }
    }
}