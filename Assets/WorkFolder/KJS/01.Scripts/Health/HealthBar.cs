using System;
using UnityEngine;
using UnityEngine.UI;
using WorkFolder.KJS._01.Scripts;

public class HealthBar : MonoBehaviour
{
    private Slider slider;
    private HealthSystem healthSystem;
    private void Awake()
    {
        slider = GetComponent<Slider>();
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.OnDamaged += OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        slider.value = healthSystem.Health;
    }
}
