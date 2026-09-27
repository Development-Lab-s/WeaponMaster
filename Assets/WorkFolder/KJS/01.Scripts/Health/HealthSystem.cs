using System;
using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts.Feedback;
using WorkFolder.KJS._01.Scripts.Health;
using WorkFolder.PAP.Scripts.Agent.Player;

namespace WorkFolder.KJS._01.Scripts
{
    public interface IHealthSystem
    {
        public int Health { get;}
        public event Action OnDamaged;
        public void Die(string ment);
    }

    public interface IDamageable
    {
        public void RpcTakeDamage(DamageData data);
    }
    public sealed class HealthSystem : NetworkModule , IHealthSystem , IDamageable
    {
        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        public int Health{ get; private set;} 
        
        private int _maxHealth;
        public event Action OnDied;
        public event Action OnDamaged;

        private IFeedable _iFeedable;

        [Networked] public bool _isDead { get; private set; }

        private bool deadEffect = true;

        [SerializeField] private ResetCharacter resetModule;
        [SerializeField] private GameObject killObject;
        
        
        
        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;
            Health = 100;
            _maxHealth = Health;
            HealthBar.Instance.ChangeBar(Health, _maxHealth);
            _iFeedable = gameObject.GetComponentInChildren<IFeedable>();
        }

        [Rpc(RpcSources.All , RpcTargets.StateAuthority)]
        public void RpcTakeDamage(DamageData data) => TakeDamage(data);

        public void TakeDamage(DamageData data)
        {
            if(data.Damage <= 0 || _isDead) return;
            
            
            Health = Mathf.Clamp(Health - data.Damage, 0, _maxHealth);
            HealthBar.Instance.ChangeBar(Health, _maxHealth);
            if (_iFeedable != null)
                _iFeedable.PlayAllFeedBacks();
            if (Health <= 0 && !_isDead)
            {
                Die();
                KillLog.Instance.RPC_Kill(data.Owner, "엄준식");
            }
        }

        public override void Render()
        {
            DeadEffect();
        }

        private void DeadEffect()
        {
            if (_isDead && deadEffect)
            {
                deadEffect = false;
                foreach (MonoBehaviour behaviour in Owner.GetComponentsInChildren<MonoBehaviour>())
                {
                    behaviour.enabled = false;
                }

                foreach (Rigidbody rb in Owner.GetComponentsInChildren<Rigidbody>())
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.gameObject.layer = LayerMask.NameToLayer("Died");
                    rb.gameObject.tag = "Untagged";
                }
                Destroy(killObject);
            }
        }

        private void OnHealthChanged()
        {
            OnDamaged?.Invoke();
        }

        public void Die(string ment = null)
        {
            if(_isDead && !HasStateAuthority) return;
            _isDead = true;
            resetModule.TryReset(ment);
            OnDied?.Invoke();
        }
    }
}