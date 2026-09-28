using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Fusion;
using S2;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.Effects;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using Random = UnityEngine.Random;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    public class WeaponCollideModule : MonoBehaviour
    {
        [SerializeField] private string[] deathMents;
        [SerializeField] private float hitCooldown = 0.25f;
        private float _damageSpeed = 8f;
        private Rigidbody _swordRb;
        private Collider[] _swordCol;
        private readonly Dictionary<IDamageable, float> _lastHitTime = new Dictionary<IDamageable, float>();
        private bool _isEnabled;
        private Player _owner;

        private float _lastSwingSpeed;

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
            _lastSwingSpeed = _swordRb.linearVelocity.magnitude;
        }
        
        
        
        private void OnCollisionEnter(Collision collision)
        {
            if (_owner == null || !_owner.HasInputAuthority) return;
            var contact = collision.contacts[0];
            if (contact.otherCollider.CompareTag("Weapon") && contact.thisCollider.CompareTag("Weapon"))
            {
                if (_owner.HasInputAuthority)
                    BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Clash, _swordRb.position + _swordRb.centerOfMass, _owner.Runner);
                _swordRb.linearVelocity = Vector3.zero;
            }
            if (collision.transform.CompareTag("Player") && contact.thisCollider.CompareTag("Weapon"))
            {
                if (contact.otherCollider.TryGetComponent(out HitBox hitBox))
                {
                    if (_lastSwingSpeed < _damageSpeed) return;
                    if (_lastHitTime.TryGetValue(hitBox.Damageable, out float t)
                        && Time.time - t < hitCooldown) return;     
                    
                    _lastHitTime[hitBox.Damageable] = Time.time;
                    DamageData data = new DamageData();
                    data.DeathMent = deathMents[Random.Range(0, deathMents.Length)];
                    data.Damage = (int)(hitBox.Part.DamageMultiplier * 3f + _lastSwingSpeed);
                    data.Owner = _owner.Runner.LocalPlayer;
                    hitBox.Damageable.RpcTakeDamage(data);
                    if (_owner.HasInputAuthority)
                        BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Hit, _swordRb.position + _swordRb.centerOfMass, _owner.Runner);
                    if (data.Damage >= 20)
                    {
                        CritEffectSpawnRequest request = new CritEffectSpawnRequest(collision.contacts[0].otherCollider.transform.position, data.Damage/10f);
                        Factories.GetFactory<CritEffect, CritEffectSpawnRequest>().Create(request);
                        if (_owner.HasInputAuthority)
                            BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Crit, _swordRb.position + _swordRb.centerOfMass, _owner.Runner);
                    }
                }
            }
        }
    }
}