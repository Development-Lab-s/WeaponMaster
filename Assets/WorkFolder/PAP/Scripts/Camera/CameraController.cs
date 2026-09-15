using System;
using DevLib.ModuleSystem;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.SUH.Scripts.LockOnSystem;

namespace WorkFolder.PAP.Scripts.Camera
{
    public interface ICameraController
    {
        public Vector3 EularAngle { get; }
        public Vector2 AimDirection { get; }
        public void SetAimDirection(Vector2 dir);
    }

    public class CameraController : NetworkModule, ICameraController
    {
        private UnityEngine.Camera _mainCam;
        private IWeaponModule _weaponModule;
        private CinemachineCamera _camera;
        private CinemachineInputAxisController _axisController;
        private Player _player;
        private Player _targetPlayer;
        private LockOnCameraController _lockOn;
        private bool _lastLock = false;
        
        public Vector3 EularAngle { get; private set; }
        public Vector2 AimDirection { get; private set; }
        public bool EquipWeapon => _weaponModule.IsEquipped;

        public override void Initialize(NetworkOwner owner)
        {
            base.Initialize(owner);
            _player = owner as Player;
        }

        public override void Spawned()
        {
            base.Spawned();
            if (!HasInputAuthority) return;
            
            _mainCam = UnityEngine.Camera.main;
            Debug.Assert(_mainCam != null, "Main camera is Null");
            _camera = (_mainCam.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineCamera);
            Debug.Assert(_camera != null, "Cinemachine camera is Null");
            _axisController = _camera.GetComponentInChildren<CinemachineInputAxisController>();
            _weaponModule = Owner.GetModule<IWeaponModule>();
            _lockOn = _mainCam.GetComponent<LockOnCameraController>();
            
            _lockOn.ExitLookOn(transform.position,transform.rotation);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CinemachineCore.GetVirtualCamera(0).Follow = transform;
        }

        public override void Render()
        {
            if (!HasInputAuthority) return;
            RotateCharacter();
            AxisControl();
            if (EquipWeapon)
            {
                FindTargetToLockOn();
            }
            else
            {
                _targetPlayer = null;
                ExitLockCamera();
            }

            if (_targetPlayer == null)
                ExitLockCamera();
        }

        private void ExitLockCamera()
        {
            if (!_lastLock) return;
            _lastLock = false;
            _lockOn.ExitLookOn(transform.position,transform.rotation);
        }

        private void FindTargetToLockOn()
        {
            if (_targetPlayer != null) return;
            float minDistance = float.MaxValue;
            
            foreach (Player plr in Player.All) 
            {
                if (plr == _player) continue;
                Vector3 aimPoint = plr.transform.position + Vector3.up * 1.0f;
                Vector3 vp = _mainCam.WorldToViewportPoint(aimPoint);
                if (!(vp is { z: > 0f, x: >= 0f and <= 1f, y: >= 0f and <= 1f })) continue;
                float sqrDistance = (plr.transform.position - _player.transform.position).sqrMagnitude;
                if (sqrDistance < minDistance)
                {
                    _targetPlayer = plr;
                    minDistance = sqrDistance;
                }
            }
            if (_targetPlayer == null) return;
            _lastLock = true;
            _lockOn.EnterLookOn(Owner.transform,_targetPlayer.transform);
        }

        private void AxisControl()
        {
            if (_axisController == null) return;

            foreach (InputAxisControllerBase<CinemachineInputAxisController.Reader>.Controller controller in _axisController.Controllers)
            {
                if (controller.Name == "Look Orbit X" || controller.Name == "Look X (Pan)")
                {
                    controller.Input.Gain = _player.SettingValue.MouseSensitivityX * (EquipWeapon ? 0.05f : 1f);
                }
                else if (controller.Name == "Look Orbit Y" || controller.Name == "Look Y (Tilt)")
                {
                    controller.Input.Gain = -_player.SettingValue.MouseSensitivityY * (EquipWeapon ? 0.05f : 1f);
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