using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WorkFolder.KJS._01.Scripts.Health
{

    public class HealthInput : MonoBehaviour
    {
        private IDamageable IDamagable;

        private void Awake()
        {
            IDamagable  = GetComponent<IDamageable>();
        }

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                Debug.Log("afaf");
                IDamagable.RpcTakeDamage(new DamageData
                {
                    Damage = 10
                });
            }
        }
    }
}