using System;
using DevLib.ServiceLocator;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public class BillboardItem : MonoBehaviour
    {
        private ICameraService _service;

        private void Start()
        {
            _service = ServiceLocator.Get<ICameraService>();
        }

        private void LateUpdate()
        {
            transform.LookAt(transform.position + _service.CurrentCamera.transform.rotation * Vector3.forward, _service.CurrentCamera.transform.rotation * Vector3.up);
        }
    }
}