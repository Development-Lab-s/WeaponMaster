using System;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Effects;

namespace WorkFolder.PAP.Scripts.MapSystem
{
    public interface IEffectManager
    {
        void RpcSetPositionAndPlayEffect(Vector3 position, Vector3 eulerAngles);
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

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RpcSetPositionAndPlayEffect(Vector3 position, Vector3 eulerAngles)
        {
            if (effectPrefab == null) return;

            BleedEffect effect = Instantiate(effectPrefab, position, Quaternion.Euler(eulerAngles));
            effect.SetPositionAndPlayEffect(position, Quaternion.Euler(eulerAngles));
        }
    }
}