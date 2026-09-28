using System;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Effects;

namespace WorkFolder.PAP.Scripts.MapSystem
{
    public interface IEffectManager
    {
        void PlayEffect(Vector3 position, Vector3 eulerAngles);
    }

    public class EffectManager : NetworkBehaviour, IEffectManager
    {
        [SerializeField] private BleedEffect effectPrefab;

        private void Awake()
        {
            ServiceLocator.Register<IEffectManager>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnRegister(this);
        }

        // 씬 NetworkObject가 러너에 붙어 있을 때만 RPC로 모두에게 보낸다. 스폰 전이거나 디스폰된
        // 상태에서 RPC를 부르면 Fusion 내부(RpcBuilder)에서 NullReferenceException이 난다.
        // 그때는 최소한 내 화면에서라도 이펙트를 보여준다.
        public void PlayEffect(Vector3 position, Vector3 eulerAngles)
        {
            if (Object != null && Object.IsValid)
                RpcSetPositionAndPlayEffect(position, eulerAngles);
            else
                SpawnEffect(position, eulerAngles);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RpcSetPositionAndPlayEffect(Vector3 position, Vector3 eulerAngles)
        {
            SpawnEffect(position, eulerAngles);
        }

        private void SpawnEffect(Vector3 position, Vector3 eulerAngles)
        {
            if (effectPrefab == null) return;

            BleedEffect effect = Instantiate(effectPrefab, position, Quaternion.Euler(eulerAngles));
            effect.SetPositionAndPlayEffect(position, Quaternion.Euler(eulerAngles));
        }
    }
}