using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class WeaponModule : NetworkModule,IAfterInitModule, IWeaponModule
    {
        [SerializeField] private GameObject targetWeapon;
        public bool IsEquipped { get; private set; }
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
            IsEquipped = isEquip;
        }
        
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !IsEquipped) return;
            _currentDirection = new Vector2(Mathf.Clamp(_currentDirection.x + _cameraController.AimDirection.x * 0.05f, -1,1)
                , Mathf.Clamp(_currentDirection.y + _cameraController.AimDirection.y * 0.05f, -1,1));
            targetWeapon.transform.position = transform.position + transform.TransformDirection(_currentDirection.normalized * moveRadius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, moveRadius);
        }
    }
}