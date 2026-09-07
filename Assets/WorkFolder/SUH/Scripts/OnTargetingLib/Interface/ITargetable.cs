using UnityEngine;
using WorkFolder.SUH.Scripts.OnTargetingLib.Class;

namespace WorkFolder.SUH.Scripts.OnTargetingLib.Interface
{
    public interface ITargetable
    {
        Transform TargetRoot { get; }
        bool IsTargetable { get; }
        bool IsAlive { get; }
        float PriorityBias{ get; }

        bool TryGetTargetPoint(
            TargetZone zone, 
            out Transform targetPoint);

    }
}
