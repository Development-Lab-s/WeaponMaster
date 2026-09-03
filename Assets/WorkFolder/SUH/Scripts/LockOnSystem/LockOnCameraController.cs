using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.SUH.Scripts.OnTargetingLib.Class;

namespace WorkFolder.SUH.Scripts.LockOnSystem
{
    public class LockOnCameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera freeLookCamera;
        [SerializeField] private CinemachineCamera lookOnCamera;
        private Transform currentFollowTarget;
        private Transform currentLookAtTarget;

        public void BindPlayer(Transform followTarget)
        {
            currentFollowTarget = followTarget;

            freeLookCamera.Target.TrackingTarget = followTarget;
            lookOnCamera.Target.TrackingTarget = followTarget;
        }

        public void EnterLookOn(Transform followTarget,Transform lookAtTarget)
        {
            lookOnCamera.Target.TrackingTarget = followTarget;
            lookOnCamera.Target.CustomLookAtTarget = true;
            lookOnCamera.Target.LookAtTarget = lookAtTarget;
            
            freeLookCamera.gameObject.SetActive(false);
            lookOnCamera.gameObject.SetActive(true);
        }

        public void ExitLookOn()
        {
            freeLookCamera.gameObject.SetActive(true);
            lookOnCamera.gameObject.SetActive(false);
        }

        public void SetLockOnTarget(Transform targetPoint)
        {
            if (!targetPoint) return;
            
            currentLookAtTarget = targetPoint;

            lookOnCamera.Target.CustomLookAtTarget = true;
            lookOnCamera.Target.LookAtTarget = currentLookAtTarget;
        }

       
    }
}