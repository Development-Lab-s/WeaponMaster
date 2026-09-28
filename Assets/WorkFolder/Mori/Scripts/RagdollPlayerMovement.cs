using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovementInput))]
public class RagdollPlayerMovement : NetworkBehaviour
{
    [Header("Speed")]
    [SerializeField, Min(0f)] private float walkSpeed = 2.8f;
    [SerializeField, Min(0f)] private float maxRunSpeed = 2.8f;
    [SerializeField, Min(0f)] private float forwardAcceleration = 3.5f;

    [Header("Camera Facing")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [SerializeField] private float modelYawOffset;
    [SerializeField, Min(0f)] private float puppetYawResponse = 20f;

    [Header("Puppet Pelvis Support")]
    [SerializeField, Min(0f)] private float pelvisLiftAcceleration = 4f;

    [Header("Ground Movement")]
    [SerializeField, Range(0f, 89f)] private float maximumSlope = 50f;
    [SerializeField, Min(0f)] private float airborneGravity = 20f;

    [Header("Slope Pelvis Drop")]
    // 비워 두면 루트 바로 아래 "Driver"를 찾는다.
    [SerializeField] private Transform driverRoot;
    [SerializeField, Min(0f)] private float maximumPelvisDrop = 0.35f;
    // 이만큼까지의 높이 차는 무시한다. 평지의 미세한 오차로 골반이 움직이지 않게 한다.
    [SerializeField, Min(0f)] private float pelvisDropDeadZone = 0.03f;
    [SerializeField, Min(0f)] private float pelvisDropResponse = 10f;

    private Rigidbody _rigidbody;
    private PlayerMovementInput _input;
    private FootTargetSolver[] _footSolvers;
    private Rigidbody _puppetPelvisBody;
    private float _puppetPelvisYawOffset;
    private float _currentForwardSpeed;
    private float _driverBaseHeight;
    private float _pelvisDrop;
    public float MaximumSpeed => Mathf.Max(walkSpeed, maxRunSpeed);
    public bool IsReadyWeapon = false;


    private Vector3 _commandedVelocity;
    private bool _hasCommandedVelocity;

    public Vector3 CurrentVelocity => _hasCommandedVelocity
        ? _commandedVelocity
        : _rigidbody.linearVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerMovementInput>();
        _footSolvers = GetComponentsInChildren<FootTargetSolver>(true);
        ResolvePuppetPelvisBody();
        _currentForwardSpeed = walkSpeed;

        if (driverRoot == null)
            driverRoot = transform.Find("Driver");
        if (driverRoot != null)
            _driverBaseHeight = driverRoot.localPosition.y;

        _rigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
        _rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (Runner == null || Object == null) return;

        bool hasWalkableGround = TryGetWalkableGround(out _);
        ApplyPuppetPelvisLift(hasWalkableGround);

        Quaternion rootRotation = HasInputAuthority
            ? _rigidbody.rotation
            : transform.rotation;
        RotatePuppetPelvis(rootRotation);

        UpdateSlopePelvisDrop();
    }

    private void UpdateSlopePelvisDrop()
    {
        if (driverRoot == null)
            return;

        float targetDrop = 0f;
        Vector3 up = transform.up;
        FootTargetSolver referenceSolver = null;
        float lowestFootHeight = float.PositiveInfinity;

        for (int i = 0; i < _footSolvers.Length; i++)
        {
            FootTargetSolver footSolver = _footSolvers[i];
            if (footSolver == null || !footSolver.HasGroundHit)
                continue;

            if (referenceSolver == null)
                referenceSolver = footSolver;
            lowestFootHeight = Mathf.Min(
                lowestFootHeight,
                Vector3.Dot(footSolver.GroundPoint, up));
        }

        if (referenceSolver != null
            && referenceSolver.TrySampleGround(
                transform.position,
                up,
                1.5f,
                1.5f,
                out Vector3 centerGround))
        {
            float deficit = Vector3.Dot(centerGround, up) - lowestFootHeight;
            targetDrop = Mathf.Clamp(
                deficit - pelvisDropDeadZone,
                0f,
                maximumPelvisDrop);
        }

        float blend = 1f - Mathf.Exp(-pelvisDropResponse * Time.fixedDeltaTime);
        _pelvisDrop = Mathf.Lerp(_pelvisDrop, targetDrop, blend);

        Vector3 localPosition = driverRoot.localPosition;
        localPosition.y = _driverBaseHeight - _pelvisDrop;
        driverRoot.localPosition = localPosition;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (PlayerHipCamera.IsCameraEnable)
        {
            // 이동 명령을 내리지 않는 동안에는 예전처럼 실제 Rigidbody 속도를 쓴다.
            _hasCommandedVelocity = false;
            return;
        }
        // 개발자 프리캠 중에는 이동 입력을 버리고 카메라를 따라 돌지도 않는다.
        // 이동 처리 자체는 계속 돌아 캐릭터가 땅을 딛고 제자리에 선다.
        bool freeCamera = WorkFolder.PAP.Scripts.Camera.DevFreeCamera.IsActive;
        Vector2 moveInput = freeCamera ? Vector2.zero : _input.MoveDirection;
        Quaternion cameraYawRotation = freeCamera
            ? _rigidbody.rotation
            : RotateTowardCamera();
        Vector3 localDirection = new Vector3(moveInput.x, 0, moveInput.y);
        bool hasWalkableGround = TryGetWalkableGround(
            out Vector3 groundNormal);
        float airborneVerticalVelocity =
            _rigidbody.linearVelocity.y
            - airborneGravity * Time.fixedDeltaTime;
        Vector3 velocity;

        if (localDirection.sqrMagnitude == 0f)
        {
            _currentForwardSpeed = walkSpeed;
            velocity = hasWalkableGround
                ? new Vector3(0f, -0.5f, 0f)
                : new Vector3(0f, airborneVerticalVelocity, 0f);
        }
        else
        {
            float speed;

            if (IsReadyWeapon)
            {
                speed = 2.4f;
                _currentForwardSpeed = speed;
            }
            else if (_input.HasForwardInput && _input.IsSprintPressed)
            {
                _currentForwardSpeed = Mathf.MoveTowards(
                    _currentForwardSpeed,
                    Mathf.Max(walkSpeed, maxRunSpeed),
                    forwardAcceleration * Time.fixedDeltaTime);
                speed = _currentForwardSpeed;
            }
            else
            {
                _currentForwardSpeed = walkSpeed;
                speed = walkSpeed;
            }


            Vector3 movementDirection =
                cameraYawRotation * localDirection.normalized;

            if (hasWalkableGround)
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(
                    movementDirection,
                    groundNormal);
                if (slopeDirection.sqrMagnitude > 0.0001f)
                    movementDirection = slopeDirection.normalized;
            }

            velocity = movementDirection * speed;
            velocity = hasWalkableGround
                ? velocity
                : new Vector3(
                    velocity.x,
                    airborneVerticalVelocity,
                    velocity.z);
        }

        _rigidbody.linearVelocity = velocity;
        _commandedVelocity = velocity;
        _hasCommandedVelocity = true;
    }

    private bool TryGetWalkableGround(out Vector3 groundNormal)
    {
        return TryGetFootGroundNormal(out groundNormal);
    }

    private bool TryGetFootGroundNormal(out Vector3 groundNormal)
    {
        Vector3 normalSum = Vector3.zero;
        int supportingFootCount = 0;

        if (_footSolvers != null)
        {
            for (int i = 0; i < _footSolvers.Length; i++)
            {
                FootTargetSolver footSolver = _footSolvers[i];
                if (footSolver == null || !footSolver.IsSupportingGround)
                    continue;

                Vector3 footNormal = footSolver.GroundNormal;
                if (Vector3.Angle(footNormal, Vector3.up) > maximumSlope)
                    continue;

                normalSum += footNormal;
                supportingFootCount++;
            }
        }

        if (supportingFootCount > 0
            && normalSum.sqrMagnitude > 0.0001f)
        {
            groundNormal = normalSum.normalized;
            return true;
        }

        groundNormal = Vector3.up;
        return false;
    }

    private Quaternion RotateTowardCamera()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return _rigidbody.rotation
                     * Quaternion.Euler(0f, -modelYawOffset, 0f);
            }

            cameraTransform = mainCamera.transform;
        }

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            return _rigidbody.rotation
                 * Quaternion.Euler(0f, -modelYawOffset, 0f);
        }

        Quaternion cameraYawRotation = Quaternion.LookRotation(
            cameraForward.normalized,
            Vector3.up);
        Quaternion targetRotation = cameraYawRotation
                                  * Quaternion.Euler(
                                      0f,
                                      modelYawOffset,
                                      0f);
        Quaternion nextRotation = Quaternion.RotateTowards(
            _rigidbody.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime);

        _rigidbody.MoveRotation(nextRotation);

        return cameraYawRotation;
    }

    private void ResolvePuppetPelvisBody()
    {
        StablePuppetJoint[] puppetJoints = FindObjectsByType<StablePuppetJoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < puppetJoints.Length; i++)
        {
            StablePuppetJoint puppetJoint = puppetJoints[i];
            if (puppetJoint == null
                || puppetJoint.name != "spine"
                || puppetJoint.AnimatedBone == null
                || !puppetJoint.AnimatedBone.IsChildOf(transform))
                continue;

            ConfigurableJoint spineJoint =
                puppetJoint.GetComponent<ConfigurableJoint>();
            if (spineJoint != null)
            {
                _puppetPelvisBody = spineJoint.connectedBody;
                if (_puppetPelvisBody != null)
                {
                    _puppetPelvisYawOffset = Mathf.DeltaAngle(
                        GetWorldYaw(_rigidbody.rotation),
                        GetWorldYaw(_puppetPelvisBody.rotation));
                }
            }

            break;
        }
    }

    private void RotatePuppetPelvis(Quaternion rootRotation)
    {
        if (_puppetPelvisBody == null)
            return;

        float desiredYaw = GetWorldYaw(rootRotation)
                         + _puppetPelvisYawOffset;
        float yawError = Mathf.DeltaAngle(
            GetWorldYaw(_puppetPelvisBody.rotation),
            desiredYaw);
        float maximumYawSpeed = Mathf.Max(90f, rotationSpeed);
        float yawSpeed = Mathf.Clamp(
            yawError * puppetYawResponse,
            -maximumYawSpeed,
            maximumYawSpeed);
        Vector3 angularVelocity = _puppetPelvisBody.angularVelocity;

        // Drive only world yaw. The pelvis stays dynamic and keeps its physical
        // pitch/roll response instead of being snapped or made kinematic.
        angularVelocity.y = yawSpeed * Mathf.Deg2Rad;
        _puppetPelvisBody.angularVelocity = angularVelocity;
    }

    private void ApplyPuppetPelvisLift(bool hasWalkableGround)
    {
        if (!hasWalkableGround
            || _puppetPelvisBody == null
            || _puppetPelvisBody.isKinematic
            || pelvisLiftAcceleration <= 0f)
            return;

        // Partially offsets gravity while grounded. This does not lock the
        // pelvis to a height, so jumps, falls, and physical reactions remain.
        _puppetPelvisBody.AddForce(
            Vector3.up * pelvisLiftAcceleration,
            ForceMode.Acceleration);
    }

    private static float GetWorldYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return rotation.eulerAngles.y;

        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
