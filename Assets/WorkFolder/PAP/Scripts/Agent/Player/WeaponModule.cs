using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class WeaponModule : NetworkModule,IAfterInitModule, IWeaponModule
    {
        
        [Header("References")]
        [SerializeField] private Transform rotTargetTrm;
        [SerializeField] private GameObject targetWeapon;
        
        [Header("Debug")] 
        [SerializeField] private float moveRadius;
        
        [Header("Settings")] 
        [SerializeField][Range(1,100)] private float weight = 1;

        private ICameraController _cameraController;
        private PlayerController _player;

        public bool IsEquipped { get; private set; }
        public Vector3 CurrentDirection { get; private set; }
        public Vector2 WeaponMoveDirection { get; private set; }
        public void AfterInit()
        {
            _cameraController = Owner.GetModule<ICameraController>();
        }

        public override void Initialize(NetworkOwner owner)
        {
            base.Initialize(owner);
            _player = owner as PlayerController;
        }

        public void EquipWeapon(bool isEquip)
        {
            IsEquipped = isEquip;
            if (IsEquipped) return;
            targetWeapon.transform.position = transform.position;
        }

        public void SetWeaponMoveDirection(Vector2 dir)
        {
            WeaponMoveDirection = dir;
        }
        
        public override void Render()
        {
            if (!HasStateAuthority) return;
            
            if (IsEquipped)
            {
                float x = Mathf.Clamp(
                    CurrentDirection.x + _cameraController.AimDirection.x * 0.005f *
                    _player.SettingValue.WeaponSensitivityX, -1, 1);
                float y = Mathf.Clamp(
                    CurrentDirection.y + _cameraController.AimDirection.y * 0.005f *
                    _player.SettingValue.WeaponSensitivityY, -1, 1);
                float z = Mathf.Lerp(1, 0, Mathf.Abs(x));
                CurrentDirection = new Vector3(x,y,z);
            }

            rotTargetTrm.localRotation = Quaternion.FromToRotation(Vector3.up,CurrentDirection);
            Debug.Log(CurrentDirection);
            //해결 해 시발 이거 왜 해결이 안돼
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, moveRadius);
        }
    }
}