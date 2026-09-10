using System;
using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts.Feedback;

namespace WorkFolder.KJS._01.Scripts
{
    public interface IHealthSystem
    {
        public int Health { get;}
        public event Action OnDamaged;
    }

    public interface IDamageable
    {
        public void RpcTakeDamage(DamageData data);
    }
    public sealed class HealthSystem : NetworkModule , IHealthSystem , IDamageable
    {
        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        // If you use the OnHealthChanged Method, Write code to update ui and other visual things.
        public int Health{ get; private set;} 
        public event Action OnDied;
        public event Action OnDamaged;

        private IFeedable _iFeedable;

        private bool _isDead = false;

        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;
            Health = 100;
            _iFeedable = gameObject.GetComponentInChildren<IFeedable>();
        }

        [Rpc(RpcSources.All , RpcTargets.StateAuthority)]
        public void RpcTakeDamage(DamageData data) => TakeDamage(data);

        public void TakeDamage(DamageData data)
        {
            if(data.Damage <= 0 || _isDead) return;
            
            Health -= data.Damage;
            if (_iFeedable != null)
                _iFeedable.PlayAllFeedBacks();
            if(Health <= 0 && !_isDead)
                Die();
        }

        private void OnHealthChanged()
        {
            OnDamaged?.Invoke();
        }

        private void Die()
        {
            if(_isDead) return;
            
            _isDead = true;
            OnDied?.Invoke();
        }
    }
}