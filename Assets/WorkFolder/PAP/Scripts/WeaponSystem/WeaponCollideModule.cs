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
using WorkFolder.PAP.Scripts.MapSystem;
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

        // 무기는 척추에 고정돼 있어 바닥·벽·소품(Default 레이어)에 걸리면 그 반동이 몸 전체를
        // 비튼다. 긴 무기일수록 끝이 자주 닿아 심해진다. 무기끼리(Weapon), 몸(Ragdoll)과의
        // 충돌은 그대로 두고 Default와의 충돌만 끈다. 모든 클라이언트에서 같게 적용된다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void IgnoreEnvironmentCollisions()
        {
            int weaponLayer = LayerMask.NameToLayer("Weapon");
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (weaponLayer >= 0 && defaultLayer >= 0)
                Physics.IgnoreLayerCollision(weaponLayer, defaultLayer, true);
        }

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
                    BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Clash, _swordRb.worldCenterOfMass, _owner.Runner);
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
                    data.ScoreMultiplier = hitBox.Part.DamageMultiplier;
                    hitBox.Damageable.RpcTakeDamage(data);
                    if (_owner.HasInputAuthority)
                        BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Hit, _swordRb.worldCenterOfMass, _owner.Runner);
                    if (data.Damage >= 25)
                    {
                        CritEffectSpawnRequest request = new CritEffectSpawnRequest(collision.contacts[0].otherCollider.transform.position, data.Damage/10f);
                        Factories.GetFactory<CritEffect, CritEffectSpawnRequest>().Create(request);
                        if (_owner.HasInputAuthority)
                            BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Crit, _swordRb.worldCenterOfMass, _owner.Runner);
                    }
                    ServiceLocator.Get<IEffectManager>().RpcSetPositionAndPlayEffect(contact.point,Quaternion.LookRotation(contact.normal).eulerAngles);
                }
            }
        }
    }
}