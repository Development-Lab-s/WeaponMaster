using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    public class WeaponCollideModule : NetworkBehaviour
    {
        [SerializeField] private Transform visualRoot;
        private Rigidbody _swordRb;
        private Collider _swordCol;
        private HashSet<IDamageable> _damagables = new HashSet<IDamageable>();
        private bool _isEnabled;

        private void Awake()
        {
            _swordRb = GetComponent<Rigidbody>();
            _swordCol = GetComponent<Collider>();
        }

        public override void Spawned()
        {
            foreach (Collider col in visualRoot.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(_swordCol, col,true);
            if (HasStateAuthority)
            {
                _swordRb.isKinematic = false;
                return;
            }
            // swordRb.isKinematic = true;
            // swordRb.interpolation = RigidbodyInterpolation.None;
        }

        
        public override void FixedUpdateNetwork()
        {
            if (_swordRb.linearVelocity.sqrMagnitude > 200)
            {
                _isEnabled = true;
            }
            else if (_isEnabled)
            {
                _isEnabled = false;
                _damagables.Clear();
            }
        }
        
        
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.CompareTag("Player"))
            {
                if (collision.transform.TryGetComponent(out HitBox hitBox) && !_damagables.Contains(hitBox.Damageable))
                {
                    DamageData data = new DamageData();
                    data.Damage = (int)(_swordRb.linearVelocity.magnitude * _swordRb.mass * hitBox.Part.DamageMultiplier);
                    Debug.Log(data.Damage);
                    hitBox.Damageable.RpcTakeDamage(data);
                    _damagables.Add(hitBox.Damageable);
                }
            }
        }
    }
}