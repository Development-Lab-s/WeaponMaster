using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.Rendering;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class WeaponModule : NetworkModule,IAfterInitModule, IWeaponModule
    {
        
        [Header("References")]
        [SerializeField] private Transform rotTargetTrm;
        [SerializeField] private GameObject targetWeapon;
        [SerializeField] private AbstractPartDataSO targetPartData;
        
        [Header("Debug")] 
        [SerializeField] private float moveRadius;
        [SerializeField] private AnimationCurve zCurve;
        
        [Header("PhysicSetting")]
        [Tooltip("관성업!")]
        [SerializeField] private float mass = 15f;
        [Tooltip("힘")]
        [SerializeField] private float baseForce = 20f;
        [Tooltip("가속변화량")]
        [SerializeField] private float rampRate = 50f;
        [Tooltip("최대가속")]
        [SerializeField] private float maxForce = 200f;
        [Tooltip("정지속도 조절")]
        [SerializeField] private float drag = 3f;

        private ICameraController _cameraController;
        private Player _player;
        private float _accumulatedForce = 0f;
        private Vector3 _fixedVelocity = Vector3.zero;
        
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
            _player = owner as Player;
            CurrentDirection = Vector3.zero;
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
        
        public Vector3 CalculateInertialVelocity(Vector3 inputDirection, Vector3 currentVelocity, float fixedDeltaTime)
        {
            if (inputDirection.sqrMagnitude > 0.001f)
            {
                _accumulatedForce += rampRate * fixedDeltaTime;
                _accumulatedForce = Mathf.Clamp(_accumulatedForce, baseForce, maxForce);
            }
            else
            {
                _accumulatedForce = 0f;
            }

            Vector3 driveForce = inputDirection.normalized * _accumulatedForce;
            Vector3 dragForce = -drag * currentVelocity;
            Vector3 netForce = driveForce + dragForce;

            Vector3 acceleration = netForce / Mathf.Max(mass, 0.001f);

            _fixedVelocity = currentVelocity + acceleration * fixedDeltaTime;

            return _fixedVelocity;
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
                    _player.SettingValue.WeaponSensitivityY, -0.8f, 1f);
                float val = Mathf.Clamp01(new Vector2(Mathf.Abs(x), Mathf.Abs(y)).magnitude);
                float z = Mathf.Lerp(1, 0f, zCurve.Evaluate(val) );
                CurrentDirection = new Vector3(x,y,z);
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
            _fixedVelocity = CalculateInertialVelocity(CurrentDirection, _fixedVelocity, Time.fixedDeltaTime);
            targetPartData.ChangePart(rotTargetTrm, CurrentDirection == Vector3.zero ? Vector3.zero : _fixedVelocity);
            //해결 해 시발 이거 왜 해결이 안돼
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, moveRadius);
        }
    }
}