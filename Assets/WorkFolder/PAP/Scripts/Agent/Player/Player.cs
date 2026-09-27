using System.Collections.Generic;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Fusion;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

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

        public static List<Player> All { get; private set; }
        public IAgentMovement AgentMover { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public ICameraController CameraController { get; private set; }
        
        public SkinnedMeshRenderer MeshRenderer;
        
        // 퓨전 2 방식의 네트워크 동기화 변수들
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
        

        [Networked, OnChangedRender(nameof(ColorChanged))] 
        public Color NetWorkedColor{ get; set;}
        
        private Color loadedColor { get; set; }
        
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
            NetRightGoalLocalRotation = inverseRoot * right.rotation;

            // 스윙 중에는 FootStepController가 솔버의 지면 스냅을 끈다. 프록시는
            // FootStepController를 끄므로 이 상태를 따로 보내지 않으면 스냅이 항상
            // 켜진 채로 남아, 들어 올린 발까지 매 프레임 바닥으로 끌어내린다.
            FootTargetSolver leftSolver = _footStepper.LeftSolver;
            FootTargetSolver rightSolver = _footStepper.RightSolver;
            if (leftSolver != null) NetLeftSwinging = !leftSolver.GroundSnapEnabled;
            if (rightSolver != null) NetRightSwinging = !rightSolver.GroundSnapEnabled;
        }

        // 프록시의 루트는 NetworkTransform이 remote time(보간된 과거 시점)으로 그린다.
        // [Networked] 프로퍼티를 그냥 읽으면 보간되지 않은 최신 스냅샷이 나오므로,
        // 몸은 과거인데 발은 현재가 되어 심어 둔 발이 속도에 비례해 뒤로 밀린다.
        // 같은 NetworkObject의 스냅샷 버퍼로 보간해 루트와 같은 시간대를 맞춘다.
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
        
        private void AttachWeaponLocal()
        {
            NetSpawnedWeapon.transform.SetParent(WeaponModule.GameObject.transform);
            NetSpawnedWeapon.transform.localPosition = NetPartData.Offset;
            NetSpawnedWeapon.transform.localEulerAngles = NetPartData.EulerAngles;

            // 퍼펫과 같은 이유: 프록시 무기도 루트에 끌려가므로 보간이 그 이동을 되돌리지 않게 한다.
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

            // 프리팹은 저장된 위치에서 생성(Awake)된 뒤 Spawn 위치로 옮겨진다. 퍼펫 강체를
            // 옮겨진 transform에 즉시 맞추고 속도를 지운다. 예전처럼 await 뒤에 하면
            // 데이터를 받는 동안 퍼펫이 옛 위치를 향해 날뛰다가 나중에 한 번 더 튄다.
            if (HasStateAuthority)
                TeleportTo(transform.position, transform.rotation);

            if (!HasStateAuthority)
            {
                Transform puppetRoot = targetPuppet != null ? targetPuppet : transform;
                foreach (StablePuppetJoint joint in
                         puppetRoot.GetComponentsInChildren<StablePuppetJoint>(true))
                    joint.SetProxyMode(true);

                // 프록시 퍼펫은 NetworkTransform이 Render에서 옮기는 루트에 끌려가며 이동한다.
                // 보간이 켜져 있으면 물리 스텝이 없는 프레임마다 Unity가 이전 물리 포즈로
                // transform을 되돌려 그 이동분을 버린다. 관절 추종이 없는 골반이 가장 많이
                // 뒤처지고, 드라이버를 따라 앞으로 당겨지는 척추가 그 위에서 앞으로 꺾인다.
                foreach (Rigidbody puppetBody in
                         puppetRoot.GetComponentsInChildren<Rigidbody>(true))
                    puppetBody.interpolation = RigidbodyInterpolation.None;
            }

            // OnChangedRender는 스폰 이후의 변경만 알린다. 나중에 들어온 클라이언트는
            // 이미 정해진 색을 변경으로 받지 못하므로 스폰 시점 값을 한 번 적용한다.
            if (!HasInputAuthority && NetWorkedColor.a > 0f)
                ColorChanged();

            if (!HasInputAuthority) return;
            playerInput.OnAttackKeyPressed += HandleAttack;
            
            playerId = AuthenticationService.Instance.PlayerId;
            _service = ServiceLocator.Get<IDataStoreService>();
            uint value = await ServiceLocator.Get<IDataStoreService>().GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
            NetWorkedColor = _bodyMaterials[value].color;
            ColorChanged();
            HairStyleIndex = await _service?.GetAsync<uint>(nameof(PlayerDataStore.HairStyle))!;
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
