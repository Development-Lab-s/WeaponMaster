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

    private Rigidbody _rigidbody;
    private PlayerMovementInput _input;
    private FootTargetSolver[] _footSolvers;
    private Rigidbody _puppetPelvisBody;
    private float _puppetPelvisYawOffset;
    private float _currentForwardSpeed;
    public float MaximumSpeed => Mathf.Max(walkSpeed, maxRunSpeed);

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerMovementInput>();
        _footSolvers = GetComponentsInChildren<FootTargetSolver>(true);
        ResolvePuppetPelvisBody();
        _currentForwardSpeed = walkSpeed;

        _rigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
        _rigidbody.constraints |=
            RigidbodyConstraints.FreezeRotationX
          | RigidbodyConstraints.FreezeRotationZ;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        Vector2 moveInput = _input.MoveDirection;
        Quaternion cameraYawRotation = RotateTowardCamera();
        Vector3 localDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        bool hasWalkableGround = TryGetWalkableGround(
            out Vector3 groundNormal);
        ApplyPuppetPelvisLift(hasWalkableGround);
        float airborneVerticalVelocity =
            _rigidbody.linearVelocity.y
            - airborneGravity * Time.fixedDeltaTime;
        Vector3 velocity;

        if (localDirection.sqrMagnitude == 0f)
        {
            _currentForwardSpeed = walkSpeed;
            velocity = hasWalkableGround
                ? Vector3.zero
                : new Vector3(0f, airborneVerticalVelocity, 0f);
        }
        else
        {
            float speed;

            if (_input.HasForwardInput && _input.IsSprintPressed)
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
        RotatePuppetPelvis(nextRotation);

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
