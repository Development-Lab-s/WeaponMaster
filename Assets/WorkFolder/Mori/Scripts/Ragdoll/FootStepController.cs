using UnityEngine;

[DefaultExecutionOrder(10)]
public sealed class FootStepController : MonoBehaviour
{
    [Header("Body")]
    [SerializeField] private Transform movementRoot;

    [Header("Goals")]
    [SerializeField] private Transform leftFootGoal;
    [SerializeField] private Transform rightFootGoal;

    [Header("Step")]
    [SerializeField] private float stepDistance = 0.2f;
    [SerializeField] private float stepAngle = 35f;
    [SerializeField] private float stepDuration = 0.3f;
    [SerializeField] private float stepHeight = 0.45f;

    [Header("Step Trajectory")]
    [SerializeField] private float liftPhase = 0.45f;
    [SerializeField] private float forwardApexProgress = 0.8f;
    [SerializeField] private float sideApexProgress = 0.5f;
    [SerializeField] private float backwardApexProgress = 0.15f;
    [SerializeField] private float backwardStepHeightScale = 0.333f;
    [SerializeField, Range(0f, 30f)] private float swingToeLiftAngle = 12f;

    [Header("Movement Prediction")]
    [SerializeField] private float minimumStepDuration = 0.2f;
    [SerializeField] private float speedForBaseDuration = 2.8f;
    [SerializeField] private float landingLeadTime = 0.07f;
    [SerializeField] private float strideLengthScale = 1.3f;
    [SerializeField, Min(1f)] private float sideStrideScale = 1.15f;
    [SerializeField, Min(1f)] private float backwardStrideScale = 1.2f;
    [SerializeField, Min(0f)] private float landingLimitScale = 0.5f;

    private FootState _left;
    private FootState _right;
    private FootState _steppingFoot;
    private Rigidbody _movementBody;
    private RagdollPlayerMovement _movementController;
    private Vector3 _landingOffset;
    private Vector3 _movementDirection;
    private Vector3 _stepDirection;
    private bool _isMoving;
    private bool _stepStartedMoving;
    private bool _lastMovedLeft;
    private float _predictedDuration;
    private float _activeStepDuration;
    private float _activeStepHeight;
    private float _stepElapsed;
    private Vector3 _stepStartPosition;
    private Vector3 _stepLiftPosition;
    private Vector3 _stepEndPosition;
    private Quaternion _stepStartRotation;
    private Quaternion _stepEndRotation;
    private float _originFootstepDistance ;

    public Transform LeftFootGoal => leftFootGoal;
    public Transform RightFootGoal => rightFootGoal;

    private void Awake()
    {
        _movementBody = movementRoot.GetComponent<Rigidbody>();
        _movementController = movementRoot.GetComponent<RagdollPlayerMovement>();
        _left = new FootState(leftFootGoal, movementRoot);
        _right = new FootState(rightFootGoal, movementRoot);
        _originFootstepDistance = stepDistance;
    }

    private void FixedUpdate()
    {
        UpdateMovementPrediction();

        bool stoppedDuringStep = _stepStartedMoving && !_isMoving;
        bool reversedDuringStep = Vector3.Dot(
            _stepDirection,
            _movementDirection) < -0.5f;

        if (_steppingFoot != null
            && (stoppedDuringStep || reversedDuringStep))
            BeginStep(_steppingFoot, true);

        if (_steppingFoot == null)
            TryStartStep();
        else
            UpdateStep();

        _left.Apply();
        _right.Apply();
    }

    public void ResetFootPlacement()
    {
        UpdateMovementPrediction();
        _steppingFoot = null;
        _left.Solver.GroundSnapEnabled = true;
        _right.Solver.GroundSnapEnabled = true;
        _left.PlantAtHome(movementRoot, _landingOffset);
        _right.PlantAtHome(movementRoot, _landingOffset);
        _left.Apply();
        _right.Apply();
    }

    private Vector3 GetMovementVelocity()
    {
        if (_movementController != null)
            return _movementController.CurrentVelocity;

        return _movementBody != null ? _movementBody.linearVelocity : Vector3.zero;
    }

    private void TryStartStep()
    {
        bool moveLeft = NeedsStep(_left);
        bool moveRight = NeedsStep(_right);

        if (!moveLeft && !moveRight)
            return;

        FootState nextFoot = moveLeft && moveRight
            ? (_lastMovedLeft ? _right : _left)
            : (moveLeft ? _left : _right);

        BeginStep(nextFoot, false);
    }

    private bool NeedsStep(FootState foot)
    {
        Vector3 homePosition = foot.GetHomePosition(
            movementRoot,
            _landingOffset);
        Vector3 offset = Vector3.ProjectOnPlane(
            homePosition - foot.Position,
            movementRoot.up);

        return offset.sqrMagnitude > stepDistance * stepDistance
            || Quaternion.Angle(
                foot.Rotation,
                foot.GetHomeRotation(movementRoot)) > stepAngle;
    }

    public void SetStepDistance(float step)
    {
        stepDistance = step;
    }

    public void SetOriginStepDistance()
    {
        stepDistance = _originFootstepDistance;
    }

    private void BeginStep(FootState foot, bool directionChanged)
    {
        _steppingFoot = foot;
        foot.Solver.GroundSnapEnabled = false;
        _stepDirection = _movementDirection;
        _stepStartedMoving = _isMoving;
        _stepElapsed = 0f;
        _stepStartPosition = foot.Position;
        _stepStartRotation = foot.Rotation;
        _stepEndPosition = foot.GetHomePosition(movementRoot, _landingOffset);
        _stepEndRotation = foot.GetHomeRotation(movementRoot);
        Vector3 localVelocity = movementRoot.InverseTransformDirection(
            GetMovementVelocity());
        bool movingBackward = localVelocity.z < 0f
                           && Mathf.Abs(localVelocity.z)
                           >= Mathf.Abs(localVelocity.x);
        _activeStepHeight = movingBackward
            ? stepHeight * backwardStepHeightScale
            : stepHeight;
        float liftTravel = !_isMoving
            ? 0.3f
            : Mathf.Abs(localVelocity.z) >= Mathf.Abs(localVelocity.x)
                ? (localVelocity.z >= 0f
                    ? forwardApexProgress
                    : backwardApexProgress)
                : sideApexProgress;
        Vector3 liftPosition = Vector3.Lerp(
            _stepStartPosition,
            _stepEndPosition,
            liftTravel);

        if (directionChanged)
        {
            float liftHeight = Mathf.Max(
                Vector3.Dot(_stepStartPosition, movementRoot.up),
                Vector3.Dot(_stepEndPosition, movementRoot.up)
                    + _activeStepHeight);
            liftPosition += movementRoot.up
                * (liftHeight - Vector3.Dot(liftPosition, movementRoot.up));
        }
        else
        {
            liftPosition += movementRoot.up * _activeStepHeight;
        }

        _stepLiftPosition = liftPosition;
        _activeStepDuration = _predictedDuration;
    }

    private void UpdateStep()
    {
        _stepElapsed += Time.deltaTime;
        float time = Mathf.Clamp01(_stepElapsed / _activeStepDuration);
        bool lifting = time < liftPhase;
        float phaseTime = lifting
            ? time / liftPhase
            : (time - liftPhase) / (1f - liftPhase);
        float phaseBlend = phaseTime * phaseTime * (3f - 2f * phaseTime);

        _steppingFoot.Position = lifting
            ? Vector3.Lerp(
                _stepStartPosition,
                _stepLiftPosition,
                phaseBlend)
            : Vector3.Lerp(
                _stepLiftPosition,
                _stepEndPosition,
                phaseBlend);

        float rotationBlend = time * time * (3f - 2f * time);
        Quaternion baseRotation = Quaternion.Slerp(
            _stepStartRotation,
            _stepEndRotation,
            rotationBlend);
        float toeLiftWeight = Mathf.Sin(time * Mathf.PI);
        Quaternion toeLiftRotation = Quaternion.AngleAxis(
            -swingToeLiftAngle * toeLiftWeight,
            movementRoot.right);
        _steppingFoot.Rotation = toeLiftRotation * baseRotation;

        if (time < 1f)
            return;

        _lastMovedLeft = _steppingFoot == _left;
        _steppingFoot.Solver.GroundSnapEnabled = true;
        _steppingFoot = null;
    }

    private void UpdateMovementPrediction()
    {
        Vector3 velocity = Vector3.ProjectOnPlane(
            GetMovementVelocity(),
            movementRoot.up);
        _isMoving = velocity.sqrMagnitude > 0.01f;
        _movementDirection = velocity.normalized;

        _predictedDuration = CalculateStepDuration(velocity.magnitude);
        float directionalStrideScale = GetDirectionalStrideScale(velocity);

        float maximumLandingOffset = float.PositiveInfinity;
        if (_movementController != null)
        {
            float maximumSpeed = _movementController.MaximumSpeed;
            maximumLandingOffset = maximumSpeed
                * (CalculateStepDuration(maximumSpeed) + landingLeadTime)
                * strideLengthScale
                * directionalStrideScale
                * landingLimitScale;
        }

        _landingOffset = Vector3.ClampMagnitude(
            velocity
                * (_predictedDuration + landingLeadTime)
                * strideLengthScale
                * directionalStrideScale,
            maximumLandingOffset);
    }

    private float GetDirectionalStrideScale(Vector3 worldVelocity)
    {
        Vector3 localVelocity = movementRoot.InverseTransformDirection(
            worldVelocity);
        float forwardSpeed = Mathf.Abs(localVelocity.z);
        float sideSpeed = Mathf.Abs(localVelocity.x);

        if (localVelocity.z < 0f && forwardSpeed >= sideSpeed)
            return backwardStrideScale;

        if (sideSpeed > forwardSpeed)
            return sideStrideScale;

        return 1f;
    }

    private float CalculateStepDuration(float speed)
    {
        float speedRatio = speed / speedForBaseDuration;

        return Mathf.Lerp(
            stepDuration,
            minimumStepDuration,
            Mathf.Clamp01(speedRatio - 1f));
    }

    private sealed class FootState
    {
        private readonly Transform _goal;
        private readonly Vector3 _homeLocalPosition;
        private readonly Quaternion _homeLocalRotation;

        public readonly FootTargetSolver Solver;

        public Vector3 Position;
        public Quaternion Rotation;

        public FootState(Transform goal, Transform root)
        {
            _goal = goal;
            Solver = goal.GetComponentInParent<FootTargetSolver>();
            _homeLocalPosition = root.InverseTransformPoint(goal.position);
            _homeLocalRotation = Quaternion.Inverse(root.rotation)
                               * goal.rotation;
            Position = goal.position;
            Rotation = goal.rotation;
        }

        public Vector3 GetHomePosition(Transform root, Vector3 offset)
        {
            return root.TransformPoint(_homeLocalPosition) + offset;
        }

        public Quaternion GetHomeRotation(Transform root)
        {
            return root.rotation * _homeLocalRotation;
        }

        public void PlantAtHome(Transform root, Vector3 offset)
        {
            Position = GetHomePosition(root, offset);
            Rotation = GetHomeRotation(root);
        }

        public void Apply()
        {
            _goal.SetPositionAndRotation(Position, Rotation);
        }
    }
}
