using DevLib.ModuleSystem;
using Fusion;
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
        [SerializeField] private Transform moveTargetTrm;
        [SerializeField] private FootStepController controller;
        [Networked] public Vector3 NetworkDirection { get; set; }
        [Networked] public float NetworkWeaponMove { get; set; }
        
        [Header("Debug")] 
        [SerializeField] private AnimationCurve zCurve;
        
        private ICameraController _cameraController;
        private Player _player;
        private float _force;
        private Rigidbody _weaponRigid;
        private ConfigurableJoint _joint;
        private float _currentWeaponMove;
        private Vector3 _bindPosition;
        
        public bool IsEquipped { get; private set; }
        public GameObject GameObject => gameObject;
        private AbstractPartDataSO _partData;
        private GameObject _weaponObject;
        private RagdollPlayerMovement _movement;
        public Vector3 CurrentDirection { get; private set; }
        public void SetPartData(AbstractPartDataSO partData)
        {
            _partData = partData;
        }

        public float WeaponMoveDirection { get; private set; }

        public override void Initialize(NetworkOwner owner)
        {
            base.Initialize(owner);
            _player = owner as Player;
            _movement = GetComponentInParent<RagdollPlayerMovement>();
            CurrentDirection = Vector3.zero;
        }

        public void AttachWeapon()
        {
            _weaponRigid = GetComponentInChildren<Rigidbody>();
            _joint = _weaponRigid.transform.GetComponent<ConfigurableJoint>();
            _weaponObject = transform.GetChild(0).gameObject;
        }
                
        public void AfterInit()
        {
            _cameraController = Owner.GetModule<ICameraController>();
            _bindPosition = moveTargetTrm.localPosition;
        }

        public void EquipWeapon(bool isEquip)
        {
            IsEquipped = isEquip;
        }

        public void SetWeaponMoveDirection(float strength)
        {
            WeaponMoveDirection = strength;
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
                float z = Mathf.Lerp(1, 0f, zCurve.Evaluate(val));
                _currentWeaponMove = Mathf.Clamp(_currentWeaponMove + WeaponMoveDirection * 20f * Runner.DeltaTime,-1f,1f);
                CurrentDirection = new Vector3(x, y, z);
            }
            else
            {
                CurrentDirection = Vector3.zero;
                _currentWeaponMove = 0f;
            }

            _movement.IsReadyWeapon = IsEquipped;
        }

        public override void FixedUpdateNetwork()
        {
            // 부위별로 돌아가는거 다르게 할 예정임.
            // rotTargetTrm.localRotation = Quaternion.FromToRotation(Vector3.up,CurrentDirection);
            // _joint.targetRotation = Quaternion.Euler(0,CurrentDirection.x * 90f,0);
            // if (HasStateAuthority)
            // {
            //     NetworkDirection = CurrentDirection;
            //     NetworkWeaponMove = _currentWeaponMove;
            // } 
            
            if (_partData != null)
                _partData.ChangePart(rotTargetTrm, CurrentDirection, moveTargetTrm, _bindPosition,_currentWeaponMove);
        } 

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
        }
    }
}