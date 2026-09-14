using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class WeaponModule : NetworkModule,IAfterInitModule, IWeaponModule
    {
        
        [Header("References")]
        [SerializeField] private Transform rotTargetTrm;
        
        [Header("Debug")] 
        [SerializeField] private AnimationCurve zCurve;
        
        private ICameraController _cameraController;
        private Player _player;
        private float _force;
        private Rigidbody _weaponRigid;
        private ConfigurableJoint _joint;
        
        public bool IsEquipped { get; private set; }
        public GameObject GameObject => gameObject;
        private AbstractPartDataSO _partData;
        public Vector3 CurrentDirection { get; private set; }
        public void SetPartData(AbstractPartDataSO partData)
        {
            _partData = partData;
        }

        public Vector2 WeaponMoveDirection { get; private set; }

        public override void Initialize(NetworkOwner owner)
        {
            base.Initialize(owner);
            _player = owner as Player;
            CurrentDirection = Vector3.zero;
        }

        public void AttachWeapon()
        {
            _weaponRigid = GetComponentInChildren<Rigidbody>();
            _joint = _weaponRigid.transform.GetComponent<ConfigurableJoint>();
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
                    CurrentDirection.x + _cameraController.AimDirection.x * 0.075f *
                    _player.SettingValue.WeaponSensitivityX * Runner.DeltaTime, -1f, 1f);
                float y = Mathf.Clamp(
                    CurrentDirection.y + _cameraController.AimDirection.y * 0.075f *
                    _player.SettingValue.WeaponSensitivityY * Runner.DeltaTime, -1f, 1f);
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
            // _joint.targetRotation = Quaternion.Euler(0,CurrentDirection.x * 90f,0);
            _partData.ChangePart(rotTargetTrm, CurrentDirection);
            //해결 해 시발 이거 왜 해결이 안돼
        } 

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
        }
    }
}