using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.SUH.Scripts.OnTargetingLib.Class;
using WorkFolder.SUH.Scripts.OnTargetingLib.Interface;
using WorkFolder.SUH.Scripts.OnTargetingLib.ScriptableObject;

namespace WorkFolder.SUH.Scripts.LockOnSystem.Network
{
    public class LockOnController : NetworkBehaviour
    { 
        [SerializeField] private TargetingSettingSO settings;
        private Transform cameraPivot;
        private ITargetable currentTarget;
        private Transform currentTargetPoint;
        private TargetZone currentTargetZone;
        private LockOnCameraController cameraController;
        private Collider[] colliderBuffer;
        private bool isLockedOn;
        
        public bool ChangeTargetZone(TargetZone newZone)
        {
            if (currentTarget == null) return false;

            if (!currentTarget.TryGetTargetPoint(
                    newZone,
                    out Transform newTargetPoint))
                return false;

            currentTargetZone = newZone;
            currentTargetPoint = newTargetPoint;
            
            cameraController.SetLockOnTarget(currentTargetPoint);
            
            return true;
        }

        public override void Spawned()
        {
            if (!HasStateAuthority) return;

            cameraController =
                FindFirstObjectByType<LockOnCameraController>();

            colliderBuffer =
                new Collider[settings.maximumCandidates];
            
            cameraController.BindPlayer(cameraPivot);
        }
    }
}