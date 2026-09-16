using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovementInput))]
public class PlayerMovement : MonoBehaviour
{
    private const int MaximumGroundHits = 16;

    [Header("Speed")]
    [SerializeField, Min(0f)] private float walkSpeed = 2.8f;
    [SerializeField, Min(0f)] private float maxRunSpeed = 2.8f;
    [SerializeField, Min(0f)] private float forwardAcceleration = 3.5f;

    [Header("Camera Facing")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [SerializeField] private float modelYawOffset;

    [Header("Ground Movement")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0f)] private float groundProbeInset = 0.02f;
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.35f;
    [SerializeField, Range(0f, 89f)] private float maximumSlope = 50f;
    [SerializeField, Min(0f)] private float groundStickSpeed = 1f;
    [SerializeField, Min(0f)] private float airborneGravity = 20f;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsule;
    private PlayerMovementInput _input;
    private FootTargetSolver[] _footSolvers;
    private float _currentForwardSpeed;
    private readonly RaycastHit[] _groundHits =
        new RaycastHit[MaximumGroundHits];

    public float MaximumSpeed => Mathf.Max(walkSpeed, maxRunSpeed);

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _input = GetComponent<PlayerMovementInput>();
        _footSolvers = GetComponentsInChildren<FootTargetSolver>(true);
        _currentForwardSpeed = walkSpeed;

        _rigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
        _rigidbody.constraints |=
            RigidbodyConstraints.FreezeRotationX
          | RigidbodyConstraints.FreezeRotationZ;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = _input.MoveDirection;
        Quaternion cameraYawRotation = RotateTowardCamera();
        Vector3 localDirection = new Vector3(moveInput.x, 0f, moveInput.y);
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
                ? -groundNormal * groundStickSpeed
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
                ? velocity - groundNormal * groundStickSpeed
                : new Vector3(
                    velocity.x,
                    airborneVerticalVelocity,
                    velocity.z);
        }

        _rigidbody.linearVelocity = velocity;
    }

    private bool TryGetWalkableGround(out Vector3 groundNormal)
    {
        if (TryGetFootGroundNormal(out groundNormal))
            return true;

        return TryGetRootGroundNormal(out groundNormal);
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

    private bool TryGetRootGroundNormal(out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;
        if (_capsule == null || !_capsule.enabled)
            return false;

        Bounds bounds = _capsule.bounds;
        float radius = Mathf.Max(
            0.01f,
            Mathf.Min(bounds.extents.x, bounds.extents.z)
                - groundProbeInset);
        Vector3 origin = new Vector3(
            bounds.center.x,
            bounds.min.y + radius + groundProbeInset,
            bounds.center.z);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            Vector3.down,
            _groundHits,
            groundProbeDistance + groundProbeInset,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        bool foundGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundHits[i];
            if (hit.collider == null
                || hit.collider.attachedRigidbody == _rigidbody
                || Vector3.Angle(hit.normal, Vector3.up) > maximumSlope
                || hit.distance >= nearestDistance)
                continue;

            Rigidbody hitBody = hit.collider.attachedRigidbody;
            if (hitBody != null && !hitBody.isKinematic)
                continue;

            nearestDistance = hit.distance;
            groundNormal = hit.normal;
            foundGround = true;
        }

        return foundGround;
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
}
