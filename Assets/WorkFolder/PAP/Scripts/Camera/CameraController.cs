using System;
using DevLib.ModuleSystem;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
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
        private IWeaponModule _weaponModule;
        private CinemachineInputAxisController _axisController;
        
        public Vector3 EularAngle { get; private set; }
        public Vector2 EularCamera { get; private set; }
        public Vector2 AimDirection { get; private set; }
        public bool EquipWeapon => _weaponModule.IsEquipped;
        
        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void Start()
        {
            _mainCam = UnityEngine.Camera.main;
            _axisController = (_mainCam.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineCamera).GetComponentInChildren<CinemachineInputAxisController>();
            _weaponModule = Owner.GetModule<IWeaponModule>();
        }

        public override void Spawned()
        {
            base.Spawned();
            if (!HasStateAuthority) return;
            CinemachineCore.GetVirtualCamera(0).Follow = transform;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            if (!HasStateAuthority) return;
        }

        private void Update()
        {
            RotateCharacter();
            AxisControl();
        }

        private void AxisControl()
        {
            if (_axisController == null) return;

            foreach (var controller in _axisController.Controllers)
            {
                if (controller.Name == "Look Orbit X" || controller.Name == "Look X (Pan)")
                {
                    controller.Input.Gain = values.MouseSensitivityX * (EquipWeapon ? 0.1f : 1f);
                }
                else if (controller.Name == "Look Orbit Y" || controller.Name == "Look Y (Tilt)")
                {
                    controller.Input.Gain = values.MouseSensitivityY * (EquipWeapon ? 0.1f : 1f);
                }
            }
        }

        private void RotateCharacter()
        {
            EularAngle = _mainCam.transform.eulerAngles;
        }

        public void SetAimDirection(Vector2 dir)
        {
            AimDirection = dir;
        }

    }
}