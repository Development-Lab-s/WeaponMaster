using System;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    public class WeaponCollideModule : NetworkBehaviour
    {
        [SerializeField] private Transform visualRoot;
        private Rigidbody swordRb;
        private Collider swordCol;

        private void Awake()
        {
            swordRb = GetComponent<Rigidbody>();
            swordCol = GetComponent<Collider>();
        }

        public override void Spawned()
        {
            foreach (Collider col in visualRoot.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(swordCol, col,true);
            if (HasStateAuthority)
            {
                swordRb.isKinematic = false;
                return;
            }
            //
            // swordRb.isKinematic = true;
            // swordRb.interpolation = RigidbodyInterpolation.None;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                IDamagable damagable = other.GetComponentInParent<IDamagable>();
                if (damagable != null)
                {
                    damagable
                }
            }
        }
    }
}