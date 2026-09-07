using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.SUH.Scripts.OnTargetingLib.Class;

namespace WorkFolder.SUH.Scripts.LockOnSystem
{
    public class LockOnCameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera freeLookCamera;
        [SerializeField] private CinemachineCamera lookOnCamera;

        public void EnterLookOn(Transform trackingTarget,Transform lookAtTarget)
        {
            lookOnCamera.Target.TrackingTarget = trackingTarget;
            lookOnCamera.Target.CustomLookAtTarget = true;
            lookOnCamera.Target.LookAtTarget = lookAtTarget;
            
            freeLookCamera.gameObject.SetActive(false);
            lookOnCamera.gameObject.SetActive(true);
        }

        public void ExitLookOn(Vector3 currentPosition, Quaternion currentRotation)
        {
            freeLookCamera.ForceCameraPosition(currentPosition,currentRotation);
            freeLookCamera.gameObject.SetActive(true);
            lookOnCamera.gameObject.SetActive(false);
        }
       
    }
}