using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;
using WorkFolder.PAP.Scripts.Agent.Player;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    public class WeaponCollideModule : MonoBehaviour
    {
        private Rigidbody _swordRb;
        private Collider[] _swordCol;
        private HashSet<IDamageable> _damagables = new HashSet<IDamageable>();
        private bool _isEnabled;
        private NetworkOwner _owner;

        private void Awake()
        {
            _swordRb = GetComponent<Rigidbody>();
            _swordCol = GetComponentsInChildren<Collider>();
        }

        private void Start()
        {
            _owner = GetComponentInParent<Player>();
            foreach (Collider col in _owner.GetComponentsInChildren<Collider>())
            {
                foreach (Collider swordCol in _swordCol)
                {
                    Physics.IgnoreCollision(swordCol, col,true);
                }
            }
        }

        
        private void FixedUpdate()
        {
            if (_swordRb.linearVelocity.sqrMagnitude > 150)
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
            if (_owner == null || !_owner.HasInputAuthority || !_isEnabled) return;
            var contact = collision.contacts[0];
            if (collision.transform.CompareTag("Player") && contact.thisCollider.CompareTag("Weapon"))
            {
                if (contact.otherCollider.TryGetComponent(out HitBox hitBox) && !_damagables.Contains(hitBox.Damageable))
                {
                    DamageData data = new DamageData();
                    data.Damage = (int)(hitBox.Part.DamageMultiplier * 4f + _swordRb.linearVelocity.magnitude);
                    hitBox.Damageable.RpcTakeDamage(data);
                    _damagables.Add(hitBox.Damageable); 
                }
            }
        }
    }
}