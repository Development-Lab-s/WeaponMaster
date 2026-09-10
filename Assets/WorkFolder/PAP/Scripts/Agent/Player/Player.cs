using System.Collections.Generic;
using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    // 포톤 퓨전 동기화를 위한 파트 데이터 구조체 (INetworkStruct 필수)
    public struct NetworkedPartData : INetworkStruct
    {
        public Vector2 DriveX;
        public Vector3 Offset;
        public Vector3 EulerAngles;
    }

    public class Player : NetworkOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        [field:SerializeField] public SettingValueSO SettingValue;

        public static List<Player> All { get; private set; }
        public IAgentMovement AgentMover { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public ICameraController CameraController { get; private set; }
        
        // 퓨전 2 방식의 네트워크 동기화 변수들
        [Networked] private NetworkBool NetCanInitialize { get; set; }
        [Networked] private NetworkedPartData NetPartData { get; set; }
        [Networked] private NetworkId NetTargetPartId { get; set; } // Transform 대신 NetworkId 저장
        [Networked] private NetworkObject NetSpawnedWeapon { get; set; } // 스폰된 무기 참조 공유

        private GameObject _weaponPrefab;

        private void Start()
        {
            if (All == null) All = new List<Player>();
            All.Add(this);
        }

        private void OnDestroy() => All.Remove(this);

        protected override void InitializeModules()
        {
            base.InitializeModules();
            AgentMover = GetModule<IAgentMovement>();
            WeaponModule = GetModule<IWeaponModule>();
            CameraController = GetModule<ICameraController>();
        }

        public void SetInitialize(PartOffsetDataSO partData, NetworkObject targetPartNetObj, GameObject weaponPrefab)
        {
            if (!HasStateAuthority) return;

            _weaponPrefab = weaponPrefab;
            NetTargetPartId = targetPartNetObj.Id;

            NetPartDataId = DataManager.Instance.GetIdBySO(partData.bodyPartData);

            NetPartData = new NetworkedPartData 
            { 
                DriveX = partData.driveX, 
                Offset = partData.offset, 
                EulerAngles = partData.eulerAngles 
            };
            NetTargetPartId = targetPartNetObj.Id;
            NetCanInitialize = true;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && NetCanInitialize && NetSpawnedWeapon == null)
            {
                NetSpawnedWeapon = Runner.Spawn(_weaponPrefab, inputAuthority: Object.InputAuthority);
            }
        }

        public override void Render()
        {
            if (NetSpawnedWeapon != null && NetSpawnedWeapon.transform.parent == null)
            {
                AttachWeaponLocal();
            }
        }

        private void AttachWeaponLocal()
        {
            NetSpawnedWeapon.transform.SetParent(WeaponModule.GameObject.transform);
            NetSpawnedWeapon.transform.localPosition = NetPartData.Offset;
            NetSpawnedWeapon.transform.localEulerAngles = NetPartData.EulerAngles;
            
            Transform rotPart = FindChildHelper(transform, "spine");
            if (rotPart != null && rotPart.TryGetComponent<ConfigurableJoint>(out var joint))
            {
                if (NetPartData.DriveX == Vector2.zero)
                {
                    joint.angularXMotion = ConfigurableJointMotion.Free;
                }
                joint.lowAngularXLimit = new SoftJointLimit { limit = NetPartData.DriveX.x };
                joint.highAngularXLimit = new SoftJointLimit { limit = NetPartData.DriveX.y };
            }

            if (Runner.TryFindObject(NetTargetPartId, out var targetNetObj))
            {
                if (NetSpawnedWeapon.TryGetComponent<Joint>(out var weaponJoint))
                {
                    weaponJoint.connectedBody = targetNetObj.GetComponent<Rigidbody>();
                }
            }

            WeaponModule.AttachWeapon();
        }

        Transform FindChildHelper(Transform current, string targetName)
        {
            if (current.name == targetName) return current;
            foreach (Transform child in current)
            {
                Transform found = FindChildHelper(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        public override void Spawned()
        {
            if (!HasInputAuthority) return;
            playerInput.OnAttackKeyPressed += HandleAttack;
        }

        private void HandleAttack(bool isAttack) => WeaponModule.EquipWeapon(isAttack);

        private void Update()
        {
            if (!HasInputAuthority) return;
            AgentMover.SetMovementDirection(playerInput.MoveDirection);
            CameraController.SetAimDirection(playerInput.AimDirection);
            WeaponModule.SetWeaponMoveDirection(playerInput.WeaponMoveDirection);
        }
        
        [Networked] 
        [OnChangedRender(nameof(OnPartDataIdChanged))]
        private int NetPartDataId { get; set; } = -1;
        private void OnPartDataIdChanged()
        {
            if (NetPartDataId == -1) return;

            AbstractPartDataSO localSO = DataManager.Instance.GetSOById(NetPartDataId);

            if (localSO != null && WeaponModule != null)
            {
                WeaponModule.SetPartData(localSO);
            }
        }

    }
}
