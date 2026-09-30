using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Fusion;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
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
    public struct NetworkedPartData : INetworkStruct
    {
        public Vector2 DriveX;
        public Vector3 Offset;
        public Vector3 Offset2;
        public Vector3 EulerAngles;
    }

    public class Player : NetworkOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        [field:SerializeField] public SettingValueSO SettingValue;
        [SerializeField]private Transform targetPuppet;
        [SerializeField] private TextMeshPro nameTag;
        [SerializeField] private GameObject nameTitleObj;
        [SerializeField] private GameObject cameraTarget;

        public static List<Player> All { get; private set; }
        public IAgentMovement AgentMover { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public ICameraController CameraController { get; private set; }
        
        public SkinnedMeshRenderer MeshRenderer;
        
        [Networked] private NetworkBool NetCanInitialize { get; set; }
        [Networked] private NetworkedPartData NetPartData { get; set; }
        [Networked] private NetworkId NetTargetPartId { get; set; } 
        [Networked] private NetworkObject NetSpawnedWeapon { get; set; } 
        [Networked] private NetworkObject NetSpawnedWeapon2 { get; set; }

        private bool HasSecondWeapon => NetPartData.Offset2 != Vector3.zero;

        [SerializeField, Range(0.1f, 1f)] private float dualWeaponScale = 0.75f;

        [SerializeField, Range(0f, 90f)] private float dualWeaponSpreadAngle = 15f;

        [SerializeField, Min(1f)] private float maximumWeaponInertiaRatio = 8f;

        [SerializeField, Min(0.1f)] private float weaponMaxDepenetrationVelocity = 3f;

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
                Offset2 = partData.offset2,
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
            if (HasStateAuthority && NetCanInitialize)
            {
                if (NetSpawnedWeapon == null)
                    NetSpawnedWeapon = SpawnWeaponAt(NetPartData.Offset);

                if (HasSecondWeapon && NetSpawnedWeapon2 == null)
                    NetSpawnedWeapon2 = SpawnWeaponAt(NetPartData.Offset2);
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
            NetRightGoalLocalRotation = inverseRoot * right.rotation;

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
                AttachWeaponLocal(NetSpawnedWeapon, NetPartData.Offset);

            if (NetSpawnedWeapon2 != null && NetSpawnedWeapon2.transform.parent == null)
                AttachWeaponLocal(NetSpawnedWeapon2, NetPartData.Offset2);
            OnPartDataIdChanged();

            ApplyFootGoals();
        }

        public Color GetBodyPaletteColor(uint index)
        {
            if (index >= _bodyMaterials.Length || _bodyMaterials[index] == null)
                throw new System.ArgumentOutOfRangeException(nameof(index));
            return FaceDrawShared.GetColor(_bodyMaterials[index]);
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
            if (!hasState)
                return;

            if (ServiceLocator.TryGet(out ILeaderboardGiver giver))
                giver.RemovePlayer(playerId.Value);
        }

        private Quaternion GetWeaponLocalRotation(Vector3 localOffset)
        {
            Quaternion baseRotation = Quaternion.Euler(NetPartData.EulerAngles);
            if (!HasSecondWeapon || Mathf.Approximately(localOffset.x, 0f))
                return baseRotation;

            float spread = Mathf.Sign(localOffset.x) * dualWeaponSpreadAngle;
            return Quaternion.AngleAxis(spread, Vector3.up) * baseRotation;
        }

        private NetworkObject SpawnWeaponAt(Vector3 localOffset)
        {
            Transform socket = WeaponModule.GameObject.transform;
            return Runner.Spawn(
                _weaponPrefab,
                socket.TransformPoint(localOffset),
                socket.rotation * GetWeaponLocalRotation(localOffset),
                inputAuthority: Object.InputAuthority);
        }

        private void AttachWeaponLocal(NetworkObject weapon, Vector3 localOffset)
        {
            weapon.transform.SetParent(WeaponModule.GameObject.transform);

            if (HasSecondWeapon)
            {
                weapon.transform.localScale *= dualWeaponScale;

                foreach (Rigidbody weaponBody in weapon.GetComponentsInChildren<Rigidbody>(true))
                    weaponBody.mass *= dualWeaponScale;
            }

            weapon.transform.localPosition = localOffset;
            weapon.transform.localRotation = GetWeaponLocalRotation(localOffset);

            foreach (Rigidbody weaponBody in
                     weapon.GetComponentsInChildren<Rigidbody>(true))
            {
                weaponBody.interpolation = HasStateAuthority
                    ? RigidbodyInterpolation.Interpolate
                    : RigidbodyInterpolation.None;

                weaponBody.maxDepenetrationVelocity = weaponMaxDepenetrationVelocity;


                weaponBody.position = weaponBody.transform.position;
                weaponBody.rotation = weaponBody.transform.rotation;
                if (!weaponBody.isKinematic)
                {
                    weaponBody.linearVelocity = Vector3.zero;
                    weaponBody.angularVelocity = Vector3.zero;
                }
            }
            Physics.SyncTransforms();

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
                if (weapon.TryGetComponent<Joint>(out var weaponJoint))
                {
                    Rigidbody partBody = targetNetObj.GetComponent<Rigidbody>();

                    if (weaponJoint is ConfigurableJoint configurableJoint)
                    {
                        configurableJoint.projectionMode = JointProjectionMode.PositionAndRotation;
                        configurableJoint.projectionDistance = 0.3f;
                        configurableJoint.projectionAngle = 30f;

                        if (!weapon.TryGetComponent(out WeaponEdgeAligner edgeAligner))
                            edgeAligner = weapon.gameObject.AddComponent<WeaponEdgeAligner>();

                        edgeAligner.Bind(configurableJoint, partBody, GetComponent<Rigidbody>());
                    }

                    weaponJoint.connectedBody = partBody;

                    if (!weapon.TryGetComponent(out WeaponInertiaLimiter inertiaLimiter))
                        inertiaLimiter = weapon.gameObject.AddComponent<WeaponInertiaLimiter>();
                    inertiaLimiter.Bind(weaponJoint, partBody, maximumWeaponInertiaRatio);
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
                string savedName = await _service.GetAsync<string>(nameof(PlayerDataStore.PlayerName));
                if (this == null || Object == null || !Object.IsValid || !HasStateAuthority) return;
                playerName = savedName;
                uint value = await _service.GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
                if (this == null || Object == null || !Object.IsValid || !HasStateAuthority) return;
                NetWorkedColor = GetBodyPaletteColor(value);
                HairStyleIndex = await _service?.GetAsync<uint>(nameof(PlayerDataStore.HairStyle))!;
                if (this == null || Object == null || !Object.IsValid || !HasStateAuthority) return;
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

            ColorChanged();
            NameChanged();
        }

        private void Start()
        {
            if (!HasStateAuthority)
                Destroy(cameraTarget);
        }

        [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
        public void RpcKilled(int score)
        {
            _ = UpdateKillCountAsync(score);
        }
        public async Task UpdateKillCountAsync(int score)
        {
            uint currentKill = await ServiceLocator.Get<IDataStoreService>()
                .GetAsync<uint>(nameof(PlayerDataStore.KillCount));
            uint currentScore = await ServiceLocator.Get<IDataStoreService>()
                .GetAsync<uint>(nameof(PlayerDataStore.Score));
            await ServiceLocator.Get<IDataStoreService>()
                .SetAsync<uint>(nameof(PlayerDataStore.KillCount), currentKill + 1);
            await ServiceLocator.Get<IDataStoreService>()
                .SetAsync<uint>(nameof(PlayerDataStore.Score), (uint)(currentScore + score + 20 * KillStreak));
            await ServiceLocator.Get<IDataStoreService>().SaveAsync();
            KillStreak += 1;
        }

        public async Task UpdateScoreDownAsync()
        {
            uint currentScore = await  ServiceLocator.Get<IDataStoreService>()
                .GetAsync<uint>(nameof(PlayerDataStore.Score));
            await  ServiceLocator.Get<IDataStoreService>()
                .SetAsync(nameof(PlayerDataStore.Score), currentScore - 50);
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
