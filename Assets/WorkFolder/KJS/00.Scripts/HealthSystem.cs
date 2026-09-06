using System;
using DevLib.ModuleSystem;
using Fusion;

namespace WorkFolder.KJS._00.Scripts
{
    public interface IHealthSystem
    {
        public int Health { get;}
        public void RpcTakeDamage(DamageData data);
    }

    public interface IDamagable
    {
        
    }
    public sealed class HealthSystem : NetworkModule , IHealthSystem , IDamagable
    {
        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        // If you use the OnHealthChanged Method, Write code to update ui and other visual things.
        public int Health{ get; private set;} 
        
        public event Action OnDied;
        public event Action OnDamaged;

        private bool _isDead = false;

        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;
            Health = 100;
        }

        [Rpc(RpcSources.All , RpcTargets.StateAuthority)]
        public void RpcTakeDamage(DamageData data) => TakeDamage(data);

        private void TakeDamage(DamageData data)
        {
            if(data.Damage <= 0 || _isDead) return;
            
            Health -= data.Damage;
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