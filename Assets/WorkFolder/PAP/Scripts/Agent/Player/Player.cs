using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Fusion;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.GameSystem.KillSystem;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.PAP.Scripts.WeaponSystem;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

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
        [SerializeField]private Transform targetPuppet;
        [SerializeField] private TextMeshPro nameTag;
        [SerializeField] private GameObject nameTitleObj;

        public static List<Player> All { get; private set; }
        public IAgentMovement AgentMover { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public ICameraController CameraController { get; private set; }
        
        public SkinnedMeshRenderer MeshRenderer;
        
        [Networked] private NetworkBool NetCanInitialize { get; set; }
        [Networked] private NetworkedPartData NetPartData { get; set; }
        [Networked] private NetworkId NetTargetPartId { get; set; } // Transform 대신 NetworkId 저장
        [Networked] private NetworkObject NetSpawnedWeapon { get; set; } // 스폰된 무기 참조 공유

        [Networked] public Vector3 NetLeftGoalLocal { get; private set; }
        [Networked] public Vector3 NetRightGoalLocal { get; private set; }
        [Networked] public Quaternion NetLeftGoalLocalRotation { get; private set; }
        [Networked] public Quaternion NetRightGoalLocalRotation { get; private set; }
        [Networked] public NetworkBool NetLeftSwinging { get; private set; }
        [Networked] public NetworkBool NetRightSwinging { get; private set; }

        [Networked] public NetworkString<_32> playerId { get; set; }
        
        [Networked, OnChangedRender(nameof(NameChanged))] public NetworkString<_32> playerName { get; set; }

        [Networked, OnChangedRender(nameof(ColorChanged))] 
        public Color NetWorkedColor{ get; set;}

        [Networked] public int KillStreak { get; set; }
        
        private uint HairStyleIndex { get; set; }
        
        private MaterialPropertyBlock _mpb;
        private IDataStoreService _service;
        
        private GameObject _weaponPrefab;
        private FootStepController _footStepper;

        [SerializeField]private Material[] _bodyMaterials;
        private void OnEnable()
        {
            if (All == null) All = new List<Player>();
            All.Add(this);
        }

        private void OnDisable() => All.Remove(this);

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

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
            {
                rb.position = rb.transform.position;
                rb.rotation = rb.transform.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            
            foreach (FootStepController stepper in GetComponentsInChildren<FootStepController>(true))
                stepper.ResetFootPlacement();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && NetCanInitialize && NetSpawnedWeapon == null)
            {
                NetSpawnedWeapon = Runner.Spawn(_weaponPrefab, inputAuthority: Object.InputAuthority);
            }

            CaptureFootGoals();
        }

        private bool TryGetFootGoals(out Transform left, out Transform right)
        {
            left = null;
            right = null;

            if (_footStepper == null)
            {
                _footStepper = GetComponentInChildren<FootStepController>(true);
                if (_footStepper == null) return false;
            }

            left = _footStepper.LeftFootGoal;
            right = _footStepper.RightFootGoal;

            return left != null && right != null;
        }

        private void CaptureFootGoals()
        {
            if (!HasStateAuthority) return;
            if (!TryGetFootGoals(out Transform left, out Transform right)) return;

            Quaternion inverseRoot = Quaternion.Inverse(transform.rotation);

            NetLeftGoalLocal = transform.InverseTransformPoint(left.position);
            NetLeftGoalLocalRotation = inverseRoot * left.rotation;
            NetRightGoalLocal = transform.InverseTransformPoint(right.position);

            FootTargetSolver leftSolver = _footStepper.LeftSolver;
            FootTargetSolver rightSolver = _footStepper.RightSolver;
            if (leftSolver != null) NetLeftSwinging = !leftSolver.GroundSnapEnabled;
            if (rightSolver != null) NetRightSwinging = !rightSolver.GroundSnapEnabled;
        }

        private void ApplyFootGoals()
        {
            if (HasStateAuthority) return;
            if (!TryGetFootGoals(out Transform left, out Transform right)) return;

            if (_footStepper.enabled)
                _footStepper.enabled = false;

            Vector3 leftLocal;
            Vector3 rightLocal;
            Quaternion leftLocalRotation;
            Quaternion rightLocalRotation;
            bool leftSwinging;
            bool rightSwinging;

            NetworkBehaviourBufferInterpolator interpolator =
                new NetworkBehaviourBufferInterpolator(this);

            if (interpolator)
            {
                leftLocal = interpolator.Vector3(nameof(NetLeftGoalLocal));
                rightLocal = interpolator.Vector3(nameof(NetRightGoalLocal));
                leftLocalRotation = interpolator.Quaternion(nameof(NetLeftGoalLocalRotation));
                rightLocalRotation = interpolator.Quaternion(nameof(NetRightGoalLocalRotation));
                leftSwinging = interpolator.NetworkBool(nameof(NetLeftSwinging));
                rightSwinging = interpolator.NetworkBool(nameof(NetRightSwinging));
            }
            else
            {
                leftLocal = NetLeftGoalLocal;
                rightLocal = NetRightGoalLocal;
                leftLocalRotation = NetLeftGoalLocalRotation;
                rightLocalRotation = NetRightGoalLocalRotation;
                leftSwinging = NetLeftSwinging;
                rightSwinging = NetRightSwinging;
            }

            left.SetPositionAndRotation(
                transform.TransformPoint(leftLocal),
                transform.rotation * leftLocalRotation);

            right.SetPositionAndRotation(
                transform.TransformPoint(rightLocal),
                transform.rotation * rightLocalRotation);

            FootTargetSolver leftSolver = _footStepper.LeftSolver;
            FootTargetSolver rightSolver = _footStepper.RightSolver;
            if (leftSolver != null) leftSolver.GroundSnapEnabled = !leftSwinging;
            if (rightSolver != null) rightSolver.GroundSnapEnabled = !rightSwinging;
        }

        public override void Render()
        {
            if (NetSpawnedWeapon != null && NetSpawnedWeapon.transform.parent == null)
            {
                AttachWeaponLocal();
            }
            OnPartDataIdChanged();

            ApplyFootGoals();
        }

        void ColorChanged()
        {
            MeshRenderer.material.color = NetWorkedColor;
        }

        void NameChanged()
        {
            nameTag.SetText(playerName.ToString());

        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Get<ILeaderboardGiver>().RemovePlayer(playerId.Value);
        }

        private void AttachWeaponLocal()
        {
            NetSpawnedWeapon.transform.SetParent(WeaponModule.GameObject.transform);
            NetSpawnedWeapon.transform.localPosition = NetPartData.Offset;
            NetSpawnedWeapon.transform.localEulerAngles = NetPartData.EulerAngles;

            if (!HasStateAuthority)
            {
                foreach (Rigidbody weaponBody in
                         NetSpawnedWeapon.GetComponentsInChildren<Rigidbody>(true))
                    weaponBody.interpolation = RigidbodyInterpolation.None;
            }

            Transform rotPart = FindChildHelper(targetPuppet, "spine");
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
                    Rigidbody partBody = targetNetObj.GetComponent<Rigidbody>();

                    if (weaponJoint is ConfigurableJoint configurableJoint)
                    {
                        if (!NetSpawnedWeapon.TryGetComponent(out WeaponEdgeAligner edgeAligner))
                            edgeAligner = NetSpawnedWeapon.gameObject.AddComponent<WeaponEdgeAligner>();

                        edgeAligner.Bind(configurableJoint, partBody, GetComponent<Rigidbody>());
                    }

                    weaponJoint.connectedBody = partBody;
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

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetColor(Color newColor)
        {
            if (!HasStateAuthority) return;
            newColor.a = 1f;
            NetWorkedColor = newColor;
            
        }
        
        public async override void Spawned()
        {

            
            if (!HasStateAuthority && TryGetComponent(out Rigidbody rootBody))
            {
                rootBody.linearVelocity = Vector3.zero;
                rootBody.angularVelocity = Vector3.zero;
                rootBody.isKinematic = true;
                rootBody.interpolation = RigidbodyInterpolation.None;
            }

            if (HasStateAuthority)
            {
                nameTitleObj.SetActive(false);
                TeleportTo(transform.position, transform.rotation);
                
                playerInput.OnAttackKeyPressed += HandleAttack;
                KillStreak = 0;
            
                playerId = AuthenticationService.Instance.PlayerId;
                _service = ServiceLocator.Get<IDataStoreService>();
                playerName = await _service.GetAsync<string>(nameof(PlayerDataStore.PlayerName));
                uint value = await _service.GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
                NetWorkedColor = _bodyMaterials[value].color;
                ColorChanged();
                HairStyleIndex = await _service?.GetAsync<uint>(nameof(PlayerDataStore.HairStyle))!;
            }

            if (!HasStateAuthority)
            {
                Transform puppetRoot = targetPuppet != null ? targetPuppet : transform;
                foreach (StablePuppetJoint joint in
                         puppetRoot.GetComponentsInChildren<StablePuppetJoint>(true))
                    joint.SetProxyMode(true);

                foreach (Rigidbody puppetBody in
                         puppetRoot.GetComponentsInChildren<Rigidbody>(true))
                    puppetBody.interpolation = RigidbodyInterpolation.None;
            }

        }


        private void HandleAttack(bool isAttack) => WeaponModule.EquipWeapon(isAttack);

        private void Update()
        {
            if (!HasInputAuthority) return;
            CameraController.SetAimDirection(playerInput.AimDirection);
            WeaponModule.SetWeaponMoveDirection(playerInput.WeaponMoveDirection.y);
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
