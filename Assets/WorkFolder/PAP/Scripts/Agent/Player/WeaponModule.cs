using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class WeaponModule : NetworkModule,IAfterInitModule, IWeaponModule
    {
        
        [Header("References")]
        [SerializeField] private Transform rotTargetTrm;
        [SerializeField] private AbstractPartDataSO targetPartData;
        
        [Header("Debug")] 
        [SerializeField] private AnimationCurve zCurve;
        
        private ICameraController _cameraController;
        private Player _player;
        private float _force;
        
        public bool IsEquipped { get; private set; }
        public Vector3 CurrentDirection { get; private set; }
        public Vector2 WeaponMoveDirection { get; private set; }

        public override void Initialize(NetworkOwner owner)
        {
            base.Initialize(owner);
            _player = owner as Player;
            CurrentDirection = Vector3.zero;
        }
                
        public void AfterInit()
        {
            _cameraController = Owner.GetModule<ICameraController>();
        }

        public void EquipWeapon(bool isEquip)
        {
            IsEquipped = isEquip;
        }

        public void SetWeaponMoveDirection(Vector2 dir)
        {
            WeaponMoveDirection = dir;
        }
        public override void Render()
        {
            if (!HasInputAuthority) return;
            if (IsEquipped)
            {
                float x = Mathf.Clamp(
                    CurrentDirection.x + _cameraController.AimDirection.x * 0.001f *
                    _player.SettingValue.WeaponSensitivityX, -1f, 1f);
                float y = Mathf.Clamp(
                    CurrentDirection.y + _cameraController.AimDirection.y * 0.001f *
                    _player.SettingValue.WeaponSensitivityY, -1f, 1f);
                float val = Mathf.Clamp01(new Vector2(Mathf.Abs(x), Mathf.Abs(y)).magnitude);
                float z = Mathf.Lerp(1, 0f, zCurve.Evaluate(val) );
                CurrentDirection = new Vector3(x, y, z);
            }
            else 
            {
                CurrentDirection = Vector3.zero;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasInputAuthority) return; 
            //부위별로 돌아가는거 다르게 할 예정임. 
            // rotTargetTrm.localRotation = Quaternion.FromToRotation(Vector3.up,CurrentDirection);
            targetPartData.ChangePart(rotTargetTrm, CurrentDirection);
            //해결 해 시발 이거 왜 해결이 안돼
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
        }
    }
}