using System;
using DevLib.ModuleSystem;
using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class WeaponModule : NetworkModule,IAfterInitModule, IWeaponModule
    {
        [SerializeField] private GameObject targetWeapon;
        private bool _isEquipped;
        private ICameraController _cameraController;
        private Vector2 _currentDirection;

        [Header("Debug")] 
        [SerializeField] private float moveRadius;
        
        public void AfterInit()
        {
            _cameraController = Owner.GetModule<ICameraController>();
        }

        public void EquipWeapon(bool isEquip)
        {
            _isEquipped = isEquip;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !_isEquipped) return;
            _currentDirection = new Vector2(Mathf.Clamp(_currentDirection.x + _cameraController.AimDirection.x * 0.01f, -1,1)
                , Mathf.Clamp(_currentDirection.y + _cameraController.AimDirection.y * 0.01f, -1,1));
            targetWeapon.transform.position = transform.position + transform.TransformDirection(_currentDirection.normalized * moveRadius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, moveRadius);
        }
    }
}