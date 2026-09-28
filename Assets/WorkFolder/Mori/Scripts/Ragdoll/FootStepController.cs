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
    [SerializeField] private float backwardApexProgress = 0.4f;
    [SerializeField] private float backwardStepHeightScale = 0.7f;
    [SerializeField, Range(0f, 30f)] private float swingToeLiftAngle = 12f;

    [Header("Movement Prediction")]
    [SerializeField] private float minimumStepDuration = 0.2f;
    [SerializeField] private float speedForBaseDuration = 2.8f;
    // 착지 순간 발을 고관절보다 진행 방향으로 얼마나 앞에 둘지. 딛고 있는 동안
    // 몸이 이동하는 거리의 비율이며, 0.5면 딛는 구간이 고관절을 중심으로 앞뒤 대칭이 된다.
    [SerializeField, Range(0f, 1f)] private float touchdownLeadRatio = 0.5f;
    [SerializeField, Min(0f)] private float maximumTouchdownLead = 0.6f;
    // 방향 배율은 착지 선행 거리에만 곱한다. 스윙 동안의 이동 보정에는 곱하지 않는다.
    [SerializeField, Min(0f)] private float sideStrideScale = 1f;
    [SerializeField, Min(0f)] private float backwardStrideScale = 1.2f;

    [Header("Terrain")]
    // 착지 예정 지점의 지면을 찾을 때 루트 높이 기준으로 위아래 탐색 범위.
    [SerializeField, Min(0f)] private float terrainSearchHeight = 1.5f;
    // 한 걸음에 반영할 지면 높이 차의 상한. 이보다 크면 낭떠러지나 벽 윗면으로 보고 무시한다.
    [SerializeField, Min(0f)] private float maximumTerrainStep = 0.9f;
    // 착지점이 벽을 넘지 않게 검사하는 높이(루트 기준)와 굵기, 벽 앞에 남길 여유.
    // 이 높이보다 낮은 턱은 벽이 아니라 디딤판으로 보고 지형 보정에 맡긴다.
    [SerializeField, Min(0f)] private float obstacleProbeHeight = 0.5f;
    [SerializeField, Min(0.01f)] private float obstacleProbeRadius = 0.12f;
    [SerializeField, Min(0f)] private float obstacleClearance = 0.15f;

    [Header("Teleport")]
    // 한 물리 스텝에 루트가 이보다 멀리 움직이면 걷기가 아니라 순간이동으로 보고 발을 다시 심는다.
    [SerializeField, Min(0.1f)] private float teleportDistance = 1f;

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
    private float _stepForwardness;
    private Vector3 _stepStartPosition;
    private Vector3 _stepLiftPosition;
    private Vector3 _stepEndPosition;
    private Quaternion _stepStartRotation;
    private Quaternion _stepEndRotation;
    private float _originFootstepDistance ;
    private Vector3 _lastRootPosition;
    private bool _hasLastRootPosition;

    public Transform LeftFootGoal => leftFootGoal;
    public Transform RightFootGoal => rightFootGoal;
    public FootTargetSolver LeftSolver => _left != null ? _left.Solver : null;
    public FootTargetSolver RightSolver => _right != null ? _right.Solver : null;

    private void Awake()
    {
        _movementBody = movementRoot.GetComponent<Rigidbody>();
        _movementController = movementRoot.GetComponent<RagdollPlayerMovement>();
        _left = new FootState(leftFootGoal, movementRoot);
        _right = new FootState(rightFootGoal, movementRoot);
        _originFootstepDistance = stepDistance;
    }

    private void OnEnable()
    {
        _hasLastRootPosition = false;
    }

    private void FixedUpdate()
    {
        // 발 목표는 월드 좌표로 저장된다. 프리팹은 저장된 위치에서 Awake한 뒤 Spawn
        // 위치로 옮겨지므로, 그대로 두면 첫 걸음들이 옛 위치에서 날아와 다리가 뒤틀린다.
        Vector3 rootPosition = movementRoot.position;
        if (!_hasLastRootPosition
            || (rootPosition - _lastRootPosition).sqrMagnitude
               > teleportDistance * teleportDistance)
        {
            ResetFootPlacement();
            return;
        }

        _lastRootPosition = rootPosition;
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
        _lastRootPosition = movementRoot.position;
        _hasLastRootPosition = true;
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
        // 착지점과 같은 벽 제한을 걸어야 벽 앞에 선 발이 닿을 수 없는 목표를 향해
        // 제자리걸음을 끝없이 반복하지 않는다.
        Vector3 homePosition = LimitToObstacles(
            foot,
            foot.GetHomePosition(movementRoot, _landingOffset));
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
        _stepEndPosition = LimitToObstacles(foot, _stepEndPosition);
        _stepEndPosition += movementRoot.up
                          * GetTerrainHeightOffset(foot, _stepEndPosition);
        _stepEndRotation = foot.GetHomeRotation(movementRoot);

        // 발끝 들기는 진행 방향 앞쪽 성분에 비례시킨다. 방향과 상관없이 발끝만 들면
        // 뒤로 걸을 때는 진행 방향의 선두인 뒤꿈치가 바닥을 파고, 옆으로 걸을 때는
        // 이유 없이 발이 앞뒤로 기울어 뒤꿈치가 뒤로 튀어나온다.
        Vector3 stepTravel = Vector3.ProjectOnPlane(
            _stepEndPosition - _stepStartPosition,
            movementRoot.up);
        _stepForwardness = stepTravel.sqrMagnitude > 0.0001f
            ? Vector3.Dot(stepTravel.normalized, movementRoot.forward)
            : 0f;
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

    // 착지점은 몸이 스윙 동안 갈 거리만큼 앞(걷기 약 1.4 m)에 잡힌다. 벽 앞에서는 그 점이
    // 벽 속에 떨어져, 발 목표가 벽 안에 박히고 퍼펫 다리가 최대 힘으로 벽을 밀며 접촉과 싸운다.
    // 발 기본 위치에서 착지점까지 사이에 벽이 있으면 벽 앞에서 멈춘다. 벽이 없으면 그대로다.
    private Vector3 LimitToObstacles(FootState foot, Vector3 endPosition)
    {
        FootTargetSolver solver = foot.Solver;
        if (solver == null)
            return endPosition;

        Vector3 up = movementRoot.up;
        Vector3 reference = foot.GetHomePosition(movementRoot, Vector3.zero);
        Vector3 travel = Vector3.ProjectOnPlane(endPosition - reference, up);
        if (travel.sqrMagnitude < 0.0001f)
            return endPosition;

        Vector3 probeLift = up * obstacleProbeHeight;
        if (!solver.TrySweepForObstacle(
                reference + probeLift,
                reference + travel + probeLift,
                obstacleProbeRadius,
                up,
                out float obstacleDistance,
                out Vector3 wallNormal))
            return endPosition;

        // 벽까지는 그대로 가고, 남은 이동 중 벽을 파고드는 성분만 버린다. 정면이면 벽 앞에
        // 서고, 비스듬히 벽을 타고 미끄러질 때는 벽을 따라 몸과 함께 발을 옮긴다.
        Vector3 direction = travel.normalized;
        float allowed = Mathf.Max(0f, obstacleDistance - obstacleClearance);
        Vector3 blockedTravel = direction * allowed;
        Vector3 slide = Vector3.ProjectOnPlane(travel - blockedTravel, up);
        Vector3 flatNormal = Vector3.ProjectOnPlane(wallNormal, up);
        if (flatNormal.sqrMagnitude > 0.0001f)
        {
            flatNormal.Normalize();
            float intoWall = Vector3.Dot(slide, flatNormal);
            if (intoWall < 0f)
                slide -= flatNormal * intoWall;
        }
        else
        {
            slide = Vector3.zero;
        }

        Vector3 limitedTravel = blockedTravel + slide;

        // 벽을 따라 미는 구간에 모서리(다른 벽)가 있으면 거기서 멈춘다.
        if (slide.sqrMagnitude > 0.0001f
            && solver.TrySweepForObstacle(
                reference + blockedTravel + probeLift,
                reference + limitedTravel + probeLift,
                obstacleProbeRadius,
                up,
                out float slideDistance,
                out _))
        {
            limitedTravel = blockedTravel
                          + slide.normalized
                          * Mathf.Max(0f, slideDistance - obstacleClearance);
        }

        return endPosition - travel + limitedTravel;
    }

    // 착지점은 루트 높이의 평면 위에서 계산된다. 경사에서는 그 평면과 실제 지면이
    // 멀어지므로(걷기 착지점 약 1.4 m 앞, 20도면 0.5 m) 오르막에선 발이 경사면을 파고들고
    // 내리막에선 허공에 딛는다. 발의 기본 위치 아래 지면과 착지점 아래 지면의 높이 차만큼
    // 착지점을 옮겨 지형을 따르게 한다. 평지에서는 차이가 0이라 기존과 같다.
    private float GetTerrainHeightOffset(FootState foot, Vector3 endPosition)
    {
        FootTargetSolver solver = foot.Solver;
        if (solver == null)
            return 0f;

        Vector3 up = movementRoot.up;
        Vector3 referencePosition = foot.GetHomePosition(movementRoot, Vector3.zero);

        if (!solver.TrySampleGround(
                referencePosition,
                up,
                terrainSearchHeight,
                terrainSearchHeight,
                out Vector3 referenceGround)
            || !solver.TrySampleGround(
                endPosition,
                up,
                terrainSearchHeight,
                terrainSearchHeight,
                out Vector3 endGround))
            return 0f;

        // 상한을 넘는 차이는 디딜 지형이 아니라 낭떠러지나 벽이므로 기존처럼 루트 높이를 쓴다.
        float offset = Vector3.Dot(endGround - referenceGround, up);
        return Mathf.Abs(offset) <= maximumTerrainStep ? offset : 0f;
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
            -swingToeLiftAngle * toeLiftWeight * _stepForwardness,
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

        // 착지점은 스윙을 시작할 때 한 번 정해지고, 스윙하는 동안 몸은 계속 이동한다.
        // 그 이동 거리(velocity * 스윙 시간)를 착지점에 온전히 더해야 발이 닿는 순간
        // 고관절 앞에 떨어진다. 예전 공식은 이 보정분까지 상한과 방향 배율로 깎아서,
        // 걷는 속도에서는 발이 항상 몸 뒤에 착지했다. 앞으로 걸을 때는 다리가 뒤로
        // 늘어진 모양이라 티가 덜 났지만, 뒤로 걸을 때는 다리가 몸 앞에 나와 보였다.
        Vector3 swingTravel = velocity * _predictedDuration;
        Vector3 touchdownLead = Vector3.ClampMagnitude(
            velocity * (_predictedDuration * touchdownLeadRatio * directionalStrideScale),
            maximumTouchdownLead);

        _landingOffset = swingTravel + touchdownLead;
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
