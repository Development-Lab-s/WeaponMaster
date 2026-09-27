using System;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts.Feedback;
using WorkFolder.KJS._01.Scripts.Health;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.KJS._01.Scripts
{
    public interface IHealthSystem
    {
        public int Health { get;}
        public event Action OnDamaged;
        public void Die(string ment, string targetBy);
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
        public void Die(string ment)
        {
            throw new NotImplementedException();
        }

        private IFeedable _iFeedable;

        [Networked] public bool _isDead { get; private set; }

        private bool deadEffect = true;

        [SerializeField] private ResetCharacter resetModule;
        [SerializeField] private GameObject killObject;

        private Player _player;
        
        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;
            Health = 100;
            _maxHealth = Health;
            HealthBar.Instance.ChangeBar(Health, _maxHealth);
            _iFeedable = gameObject.GetComponentInChildren<IFeedable>();
            _player = Owner as Player;
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

                if (!data.IsNone)
                {
                    Player targetPlayer = Runner.GetPlayerObject(data.Owner).GetComponent<Player>();
                    Die(data.DeathMent.ToString(),targetPlayer.playerName.ToString());
                    targetPlayer.RpcKilled();
                }
                else
                    Die(data.DeathMent.ToString(),"");
                KillLog.Instance.RPC_Kill(data.Owner, _player.playerName);
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

        public void Die(string ment = null,string targetBy = "")
        {
            if(_isDead && !HasStateAuthority) return;
            _isDead = true;
            resetModule.TryReset(ment,targetBy);
            OnDied?.Invoke();
        }
    }
}