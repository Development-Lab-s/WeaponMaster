using System;
using DevLib.ModuleSystem;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Camera
{
    public interface ICameraController
    {
        public Vector3 EularAngle { get; }
        public Vector2 EularCamera { get; }
        public Vector2 AimDirection { get; }
        public void SetAimDirection(Vector2 dir);
    }

    public class CameraController : NetworkModule, ICameraController
    {
        [SerializeField] private SettingValueSO values;
        private UnityEngine.Camera _mainCam;
        private CinemachineInputAxisController _axisController;
        public Vector3 EularAngle { get; private set; }
        public Vector2 EularCamera { get; private set; }
        public Vector2 AimDirection { get; private set; }

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
            _mainCam.transform.rotation = _mainCam.transform.rotation;
            EularAngle = _mainCam.transform.eulerAngles;
        }
        
        public void SetAimDirection(Vector2 dir)
        {
            AimDirection = dir;
        }

    }
}