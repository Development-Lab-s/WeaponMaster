using System;
using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Camera
{
    public interface ICameraController
    {
        public Vector3 EularAngle { get; }
    }

    public class CameraController : NetworkModule, ICameraController
    {
        [SerializeField] private SettingValueSO values;
        private UnityEngine.Camera _mainCam;
        public Vector3 EularAngle { get; private set; }

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void Start()
        {
            _mainCam = UnityEngine.Camera.main;
        }

        private void Update()
        {
            EularAngle = _mainCam.transform.rotation.eulerAngles;
        }
    }
}