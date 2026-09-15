using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Fusion;
using S2;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.Effects;
using Random = UnityEngine.Random;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    public class WeaponCollideModule : MonoBehaviour
    {
        [SerializeField] private string[] deathMents;
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
            if (_swordRb.linearVelocity.sqrMagnitude > 250)
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
                    _damagables.Add(hitBox.Damageable);
                    DamageData data = new DamageData();
                    data.DeathMent = deathMents[Random.Range(0, deathMents.Length)];
                    data.Damage = (int)(hitBox.Part.DamageMultiplier * 4f + _swordRb.linearVelocity.magnitude);
                    data.Owner = _owner.Runner.LocalPlayer;
                    hitBox.Damageable.RpcTakeDamage(data);
                    
                    if (data.Damage >= 15)
                    {
                        CritEffectSpawnRequest request = new CritEffectSpawnRequest(collision.contacts[0].otherCollider.transform.position, data.Damage/10f);
                        Factories.GetFactory<CritEffect, CritEffectSpawnRequest>().Create(request);
                    }
                }
            }
        }
    }
}