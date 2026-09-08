using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.Health
{

    public class HealthInput : MonoBehaviour
    {
        private IDamagable IDamagable;

        private void Awake()
        {
            IDamagable  = GetComponent<IDamagable>();
        }

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                IDamagable.RpcTakeDamage(new DamageData
                {
                    Damage = 10
                });
            }
        }
    }
}