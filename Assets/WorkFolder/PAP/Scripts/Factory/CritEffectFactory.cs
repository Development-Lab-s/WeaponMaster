using System;
using UnityEngine;
using WorkFolder.PAP.Scripts.Effects;

namespace S2
{
    public class CritEffectFactory : MonoBehaviour, IFactory<CritEffect, CritEffectSpawnRequest>
    {
        [SerializeField] private CritEffect prefab;

        private void Awake()
        {
            Factories.Register<CritEffect, CritEffectSpawnRequest>(this);
        }

        private void OnDestroy()
        {
            Factories.Unregister<CritEffect, CritEffectSpawnRequest>();
        }

        public CritEffect Create(CritEffectSpawnRequest request)
        {
            CritEffect effect = Instantiate(prefab, request.Position, Quaternion.identity);
            effect.Play(request.Size);
            return effect;
        }
    }
}