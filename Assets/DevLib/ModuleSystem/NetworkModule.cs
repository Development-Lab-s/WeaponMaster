using Fusion;
using UnityEngine;

namespace DevLib.ModuleSystem
{
    public class NetworkModule : NetworkBehaviour, IModule
    {
        public NetworkOwner Owner { get; private set; }
        public virtual void Initialize(NetworkOwner owner)
        {
            Owner = owner;
        }
    }
}