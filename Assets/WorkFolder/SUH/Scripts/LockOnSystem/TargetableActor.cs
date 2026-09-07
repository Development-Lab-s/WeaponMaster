using UnityEngine;
using WorkFolder.SUH.Scripts.OnTargetingLib.Interface;
using WorkFolder.SUH.Scripts.OnTargetingLib.Class;

namespace WorkFolder.SUH.Scripts.LockOnSystem
{
    public class TargetableActor : MonoBehaviour, ITargetable
    {
        public bool IsTargetable { get; private set; } = true;
        public bool IsAlive { get; private set; } = true;
        [field: SerializeField] public float PriorityBias { get; private set; }
        
        [field: SerializeField] public Transform TargetRoot { get; private set; }
        [field: SerializeField] protected Transform HeadTarget { get; private set; }
        [field: SerializeField] protected Transform TorsoTarget { get; private set; }
        [field: SerializeField] protected Transform LeftArmTarget { get; private set; }
        [field: SerializeField] protected Transform RightArmTarget { get; private set; }
        [field: SerializeField] protected Transform LeftLegTarget { get; private set; }
        [field: SerializeField] protected Transform RightLegTarget { get; private set; }

        public bool TryGetTargetPoint(TargetZone zone, out Transform targetPoint)
        {
            targetPoint = zone switch
            {
                TargetZone.Center => TargetRoot,
                TargetZone.Head => HeadTarget,
                TargetZone.Torso => TorsoTarget,
                TargetZone.LeftArm => LeftArmTarget,
                TargetZone.RightArm => RightArmTarget,
                TargetZone.Legs => LeftLegTarget != null ? LeftLegTarget : RightLegTarget,
                _ => TorsoTarget
            };

            if (targetPoint == null)
                targetPoint = TorsoTarget != null ? TorsoTarget : TargetRoot;

            return targetPoint != null;
        }

        public void SetAlive(bool isAlive)
        {
            IsAlive = isAlive;
        }

        public void SetTargetable(bool isTargetable)
        {
            IsTargetable = isTargetable;
        }

        private void Awake()
        {
            if (TargetRoot == null)
                TargetRoot = transform;
        }

        private void Reset()
        {
            TargetRoot = transform;
            IsTargetable = true;
            IsAlive = true;
        }
    }
}
