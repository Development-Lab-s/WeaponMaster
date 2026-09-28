using UnityEngine;

[DefaultExecutionOrder(20)]
public sealed class FootTargetSolver : MonoBehaviour
{
    private const int MaximumGroundHits = 32;

    [Header("Targets")]
    [SerializeField] private Transform goal;
    [SerializeField] private Transform ikTarget;
    [SerializeField] private Transform solePoint;

    [Header("Driver Leg")]
    [SerializeField] private Transform upperLeg;
    [SerializeField] private Transform lowerLeg;
    [SerializeField] private Transform foot;

    [Header("Ground Probe")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float sphereRadius = 0.1f;
    [SerializeField] private float castUpDistance = 0.45f;
    [SerializeField] private float castDownDistance = 0.85f;
    [SerializeField] private float groundClearance = 0.02f;
    [SerializeField, Min(0f)] private float groundedDistance = 0.08f;
    [SerializeField] private float maximumSlope = 50f;
    [SerializeField] private bool alignToGround = true;

    [Header("IK Safety")]
    [SerializeField] private float maximumReachRatio = 0.98f;

    [Header("Smoothing")]
    [SerializeField] private float positionResponse = 30f;
    [SerializeField] private float rotationResponse = 30f;

    private Vector3 _targetToSoleLocal;
    private Quaternion _goalToTargetRotation;
    private readonly RaycastHit[] _groundHits = new RaycastHit[MaximumGroundHits];
    private Transform _characterRoot;
    private bool _hasGroundHit;
    private bool _isGrounded;
    private Vector3 _groundNormal = Vector3.up;
    private Vector3 _groundPoint;

    public bool GroundSnapEnabled { get; set; } = true;
    public bool HasGroundHit => _hasGroundHit;
    public bool IsGrounded => _isGrounded;
    public bool IsSupportingGround => GroundSnapEnabled && _hasGroundHit && _isGrounded;
    public Vector3 GroundNormal => _groundNormal;
    public Vector3 GroundPoint => _groundPoint;

    private void Awake()
    {
        _characterRoot = upperLeg.root;
        _targetToSoleLocal = ikTarget.InverseTransformPoint(solePoint.position);
        _goalToTargetRotation = Quaternion.Inverse(goal.rotation)
                              * ikTarget.rotation;
    }

    private void OnEnable()
    {
        Solve(true);
    }

    private void FixedUpdate()
    {
        Solve(false);
    }

    private void Solve(bool snap)
    {
        Vector3 position = goal.position;
        Quaternion rotation = goal.rotation * _goalToTargetRotation;

        _hasGroundHit = ProbeGround(position, rotation, out RaycastHit hit);
        if (_hasGroundHit && GroundSnapEnabled)
        {
            if (alignToGround)
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                         * rotation;

            position = hit.point
                     + hit.normal * groundClearance
                     - rotation * _targetToSoleLocal;
        }

        position = ClampToLegReach(position);
        float positionBlend = snap ? 1f : GetBlend(positionResponse);
        float rotationBlend = snap ? 1f : GetBlend(rotationResponse);

        ikTarget.SetPositionAndRotation(
            Vector3.Lerp(ikTarget.position, position, positionBlend),
            Quaternion.Slerp(ikTarget.rotation, rotation, rotationBlend));
    }

    private bool ProbeGround(
        Vector3 goalPosition,
        Quaternion targetRotation,
        out RaycastHit hit)
    {
        Vector3 solePosition = goalPosition
                             + targetRotation * _targetToSoleLocal;
        Vector3 origin = solePosition + Vector3.up * castUpDistance;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            sphereRadius,
            Vector3.down,
            _groundHits,
            castUpDistance + castDownDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        hit = default;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = _groundHits[i];
            if (!IsGroundCandidate(candidate.collider)
                || Vector3.Angle(candidate.normal, Vector3.up) > maximumSlope
                || candidate.distance >= nearestDistance)
                continue;

            hit = candidate;
            nearestDistance = candidate.distance;
        }

        if (float.IsPositiveInfinity(nearestDistance))
        {
            _isGrounded = false;
            _groundNormal = Vector3.up;
            _groundPoint = default;
            return false;
        }

        hit.normal = GetFaceNormal(hit);

        Vector3 currentSolePosition = ikTarget.position
                                    + ikTarget.rotation * _targetToSoleLocal;
        float soleHeight = Vector3.Dot(
            currentSolePosition - hit.point,
            hit.normal);
        _isGrounded = soleHeight <= groundedDistance;
        _groundNormal = hit.normal;
        _groundPoint = hit.point;
        return true;
    }

    // 걸음을 계획할 때 착지 예정 지점의 지면 높이를 미리 잰다. 발 목표 탐지와 같은
    // 레이어·필터·경사 제한을 쓰고, 여러 면이 걸리면 예상 높이에 가장 가까운 면을 고른다.
    // (예상 높이 위의 턱이나 다른 물체 윗면보다 발이 실제로 디딜 지면을 우선한다.)
    public bool TrySampleGround(
        Vector3 point,
        Vector3 up,
        float searchUp,
        float searchDown,
        out Vector3 groundPoint)
    {
        Vector3 origin = point + up * searchUp;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            sphereRadius,
            -up,
            _groundHits,
            searchUp + searchDown,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        groundPoint = default;
        float bestHeightError = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = _groundHits[i];
            if (!IsGroundCandidate(candidate.collider)
                || candidate.distance <= 0f
                || Vector3.Angle(candidate.normal, up) > maximumSlope)
                continue;

            float heightError = Mathf.Abs(Vector3.Dot(candidate.point - point, up));
            if (heightError >= bestHeightError)
                continue;

            groundPoint = candidate.point;
            bestHeightError = heightError;
        }

        return !float.IsPositiveInfinity(bestHeightError);
    }

    // from에서 to로 구를 밀어 가장 가까운 벽(디딜 수 없는 경사의 면)까지의 거리를 준다.
    // 디딜 수 있는 바닥·경사면은 장애물로 치지 않는다. 시작부터 겹친 면은 무시한다.
    public bool TrySweepForObstacle(
        Vector3 from,
        Vector3 to,
        float radius,
        Vector3 up,
        out float distance,
        out Vector3 normal)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        distance = length;
        normal = default;
        if (length < 0.0001f)
            return false;

        int hitCount = Physics.SphereCastNonAlloc(
            from,
            radius,
            delta / length,
            _groundHits,
            length,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        bool blocked = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = _groundHits[i];
            if (!IsGroundCandidate(candidate.collider)
                || candidate.distance <= 0f
                || Vector3.Angle(candidate.normal, up) <= maximumSlope
                || candidate.distance >= distance)
                continue;

            distance = candidate.distance;
            normal = candidate.normal;
            blocked = true;
        }

        return blocked;
    }

    // 구 캐스트가 턱·계단의 모서리에 닿으면 법선이 면이 아니라 "모서리 → 구 중심" 방향이라
    // 비스듬하다(최대 maximumSlope까지 통과). 발이 그 각도로 기울고, 이동 코드도 이 법선으로
    // 경사 보정을 해 몸이 턱에서 비틀린다. 접점 바로 위에서 같은 콜라이더에 수직 레이를 쏴
    // 실제 면의 법선을 쓴다. 면 안쪽에 닿은 경우(평지·일반 경사)는 두 법선이 같아 변화가 없다.
    private Vector3 GetFaceNormal(RaycastHit hit)
    {
        const float probeHeight = 0.1f;
        Ray ray = new Ray(hit.point + Vector3.up * probeHeight, Vector3.down);

        if (hit.collider.Raycast(ray, out RaycastHit faceHit, probeHeight * 2f)
            && Vector3.Angle(faceHit.normal, Vector3.up) <= maximumSlope)
            return faceHit.normal;

        return hit.normal;
    }

    private bool IsGroundCandidate(Collider candidate)
    {
        if (candidate == null)
            return false;

        if (_characterRoot != null
            && candidate.transform.IsChildOf(_characterRoot))
            return false;

        // The physics puppet detaches from the character root at runtime, so
        // hierarchy filtering alone is not enough. Dynamic bodies must never
        // become a foot's ground reference.
        Rigidbody body = candidate.attachedRigidbody;
        return body == null || body.isKinematic;
    }

    private Vector3 ClampToLegReach(Vector3 targetPosition)
    {
        float reach = (
            Vector3.Distance(upperLeg.position, lowerLeg.position)
          + Vector3.Distance(lowerLeg.position, foot.position))
          * maximumReachRatio;
        Vector3 offset = targetPosition - upperLeg.position;

        return offset.sqrMagnitude > reach * reach
            ? upperLeg.position + offset.normalized * reach
            : targetPosition;
    }

    private static float GetBlend(float response)
    {
        return 1f - Mathf.Exp(-response * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 soleOffset = solePoint.position - ikTarget.position;
        Vector3 solePosition = goal.position + soleOffset;
        Vector3 origin = solePosition + Vector3.up * castUpDistance;
        Vector3 end = origin
                    + Vector3.down * (castUpDistance + castDownDistance);

        Gizmos.color = IsSupportingGround
            ? Color.green
            : _hasGroundHit
                ? Color.yellow
                : Color.red;
        Gizmos.DrawWireSphere(origin, sphereRadius);
        Gizmos.DrawWireSphere(end, sphereRadius);
        Gizmos.DrawLine(origin, end);

        float legLength = Vector3.Distance(
            upperLeg.position,
            lowerLeg.position)
            + Vector3.Distance(lowerLeg.position, foot.position);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            upperLeg.position,
            legLength * maximumReachRatio);
    }
}
