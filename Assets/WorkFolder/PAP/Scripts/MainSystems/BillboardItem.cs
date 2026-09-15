using System;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public class BillboardItem : MonoBehaviour
    {
        private Transform camTransform;

        private void Start()
        {
            camTransform = UnityEngine.Camera.main.transform;
        }

        private void LateUpdate()
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);
        }
    }
}