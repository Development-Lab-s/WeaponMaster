using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.Health
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image _healthSlider;
        [SerializeField] private Image _damageSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        

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
            _healthSlider.fillAmount = 1;
            _damageSlider.fillAmount = 1;
        }

        public void ChangeBar(int currentHealth, int maxHealth)
        {
            float health = (float)currentHealth/maxHealth;
            _healthSlider.fillAmount = health;
            healthText.SetText(currentHealth.ToString());
            Sequence colorSeq = DOTween.Sequence();
            colorSeq.Append(healthText.DOColor(new Color(1,1,0,1), 0.25f));
            colorSeq.Append(healthText.DOColor(new Color(1,1,1,1), 0.25f));
            _damageTween?.Kill();
            _damageTween = DOTween.To(() => _damageSlider.fillAmount, x => _damageSlider.fillAmount = x, health,
                _duration);
        }

        private void OnDisable()
        {
            _damageTween?.Kill();
        }
    }
}