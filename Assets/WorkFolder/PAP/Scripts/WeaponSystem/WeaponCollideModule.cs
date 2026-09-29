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
        private Rigidbody _strikeBody;
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
            _strikeBody = _swordRb;

            // 날("Weapon" 태그 콜라이더)이 무기 루트가 아닌 다른 Rigidbody에 달린 무기(철퇴 공)는
            // 그 부분이 실제로 때리는 곳이다. 충돌은 그 Rigidbody 쪽으로만 오므로 중계기를 붙이고,
            // 휘두르는 속도도 그 부분으로 잰다.
            foreach (Collider part in _swordCol)
            {
                Rigidbody body = part.attachedRigidbody;
                if (body == null || body == _swordRb)
                    continue;

                if (part.CompareTag("Weapon"))
                    _strikeBody = body;

                if (!body.TryGetComponent(out WeaponHitRelay relay))
                    relay = body.gameObject.AddComponent<WeaponHitRelay>();
                relay.Bind(this);
            }
        }

        private void Start()
        {
            TryIgnoreOwnerCollisions();
        }

        // 무기는 스폰된 뒤 Player.Render에서 플레이어 밑으로 붙는다. Start가 그보다 먼저 불리면
        // 주인을 못 찾아 자기 몸과의 충돌을 끄지 못하고, 휘두를 때마다 자기 몸에 맞아 튄다.
        // 주인을 찾을 때까지 매 물리 스텝 다시 시도한다.
        private bool TryIgnoreOwnerCollisions()
        {
            if (_owner != null)
                return true;

            _owner = GetComponentInParent<Player>();
            if (_owner == null)
                return false;

            foreach (Collider col in _owner.GetComponentsInChildren<Collider>(true))
            {
                foreach (Collider swordCol in _swordCol)
                {
                    if (swordCol != null && col != null && swordCol != col)
                        Physics.IgnoreCollision(swordCol, col, true);
                }
            }

            return true;
        }


        private void FixedUpdate()
        {
            if (!TryIgnoreOwnerCollisions())
                return;

            _lastSwingSpeed = _strikeBody.linearVelocity.magnitude;
        }



        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        public void HandleCollision(Collision collision)
        {
            if (_owner == null || !_owner.HasInputAuthority) return;
            var contact = collision.contacts[0];
            if (contact.otherCollider.CompareTag("Weapon") && contact.thisCollider.CompareTag("Weapon"))
            {
                if (_owner.HasInputAuthority)
                    BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Clash, _strikeBody.worldCenterOfMass, _owner.Runner);
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
                        BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Hit, _strikeBody.worldCenterOfMass, _owner.Runner);
                    if (data.Damage >= 25)
                    {
                        CritEffectSpawnRequest request = new CritEffectSpawnRequest(collision.contacts[0].otherCollider.transform.position, data.Damage/10f);
                        Factories.GetFactory<CritEffect, CritEffectSpawnRequest>().Create(request);
                        if (_owner.HasInputAuthority)
                            BK_SoundManger.instance.PlaySfx(SfxSoundEnum.Crit, _strikeBody.worldCenterOfMass, _owner.Runner);
                    }
                    if (ServiceLocator.TryGet(out IEffectManager effectManager))
                        effectManager.PlayEffect(contact.point, Quaternion.LookRotation(contact.normal).eulerAngles);
                }
            }
        }
    }
}