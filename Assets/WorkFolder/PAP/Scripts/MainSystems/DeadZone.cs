using System;
using Fusion;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;
using WorkFolder.PAP.Scripts.WeaponSystem;
using Random = UnityEngine.Random;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public class DeadZone : MonoBehaviour
    {
        private string[] _deathMent = { "세계 밖으로 떨어졌습니다!", "공허와의 만남!", "하늘과의 접촉!", "세상으로부터 탈출하다!", "공기에게 살해당함!"};
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out HitBox hitBox))
            {
                DamageData damageData = new DamageData();
                damageData.DeathMent = _deathMent[Random.Range(0, _deathMent.Length)];
                damageData.Damage = 999999999;
                hitBox.Damageable.RpcTakeDamage(damageData);
            }
        }
    }
}