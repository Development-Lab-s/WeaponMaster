using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.Health
{
    public class HealthBar : MonoBehaviour
    {
        [field:SerializeField]public HealthSystem _healthSystem { get;private set; }
        
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

        
    }
    
    #region oldScript

    /*public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _damageSlider;
        [SerializeField] private HealthSystem _healthSystem;

        [Header("Animation")]
        [SerializeField] private float _delay = 0.15f;
        [SerializeField] private float _duration = 0.5f;

        public static HealthBar Instance;

        private Tween _damageTween;

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

        private void OnEnable()
        {
            _healthSystem.OnDamaged += ChangeBar;
        }

        private void Start()
        {
            _healthSlider.value = _healthSystem.Health;
            _damageSlider.value = _healthSystem.Health;
        }

        private void ChangeBar()
        {
            float health = _healthSystem.Health;
            _healthSlider.value = health;
            _damageTween?.Kill();
            _damageTween = _damageSlider.DOValue(health, _duration).SetDelay(_delay).SetEase(Ease.OutQuad);
        }

        private void OnDisable()
        {
            _healthSystem.OnDamaged -= ChangeBar;
            _damageTween?.Kill();
        }
    }*/

    #endregion
    
}