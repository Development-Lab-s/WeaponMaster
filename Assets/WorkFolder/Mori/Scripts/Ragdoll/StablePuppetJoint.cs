using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
[DefaultExecutionOrder(100)]
public sealed class StablePuppetJoint : MonoBehaviour
{
    private const int MinimumSolverIterations = 16;
    private const int MinimumSolverVelocityIterations = 4;
    private const float MinimumAxisMagnitudeSqr = 0.0001f;
    private const float LowerBodyPositionGain = 2.5f;

    [Header("Target")]
    [SerializeField] private Transform animatedBone;

    [Header("Strength")]
    [SerializeField, Min(0f)] private float strengthMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float dampingRatio = 0.6f;
    [SerializeField, Min(0f)] private float springPerKg = 3000f;

    [Header("Position Follow")]
    [SerializeField] private bool followPosition = true;
    [SerializeField, Min(0f)] private float positionSpring = 120f;
    [SerializeField, Range(0f, 2f)] private float positionDampingRatio = 1f;
    [SerializeField, Min(0f)] private float maximumAcceleration = 80f;
    [SerializeField, Min(0f)] private float maximumTargetSpeed = 20f;

    private ConfigurableJoint _joint;
    private Quaternion _initialLocalRotation;
    private Quaternion _initialAnimatedLocalRotation;
    private Quaternion _jointSpaceRotation;
    private Quaternion _smoothedTargetRotation;
    private float _baseSpring;
    private float _baseDamper;
    private float _currentStrength = 1f;
    private bool _useSlerpDrive;
    private Rigidbody _body;
    private Vector3 _centerOfMassOffset;
    private Vector3 _previousTargetCenterOfMass;
    private bool _hasPreviousTargetCenterOfMass;
    private bool _proxyMode;
    private Vector3 _previousPositionError;
    private bool _hasPreviousPositionError;

    public Transform AnimatedBone => animatedBone;

    // 프록시에서는 NetworkTransform이 루트 transform을 직접 옮기고, Unity는 부모
    // transform이 움직이면 자식 Rigidbody를 함께 끌고 간다. 드라이버 뼈도 같은 루트
    // 밑이라 함께 움직이므로, 월드 기준 목표 속도에는 루트 이동이 들어가지만 바디의
    // linearVelocity에는 들어가지 않는다. 그 차이만큼 댐퍼가 매 스텝 퍼펫을 밀어내어
    // 속도에 비례해 앞으로 쏠리고 요동친다. 프록시에서는 목표와 바디를 모두 transform
    // 기준으로 재고, 둘의 오차가 변하는 속도로 감쇠한다. 함께 끌려간 이동은 오차에서
    // 상쇄되므로 끌려가든 아니든 올바른 상대 속도가 나온다.
    public void SetProxyMode(bool enabled)
    {
        _proxyMode = enabled;
        _hasPreviousPositionError = false;
    }

    private void Awake()
    {
        _joint = GetComponent<ConfigurableJoint>();
        _body = GetComponent<Rigidbody>();

        ConfigureJointLimits();
        CacheInitialRotations();
        CacheCenterOfMassOffset();
        ConfigureJointDrive();
        ConfigureRigidbodySolver();
        CalculateBaseDrive();
        InitializeTargetRotation();

        SetStrength(1f);
    }

    private void FixedUpdate()
    {
        if (animatedBone == null)
            return;

        Quaternion desiredRotation = CalculateTargetRotation(animatedBone.localRotation);

        // The joint drive is already a damped physical filter. Smoothing its
        // target again adds a full extra layer of visible animation lag.
        _smoothedTargetRotation = desiredRotation;

        _joint.targetRotation = _smoothedTargetRotation;
        FollowAnimatedCenterOfMass();
    }

    public void SetStrength(float strength)
    {
        _currentStrength = Mathf.Clamp01(strength);
        JointDrive drive = CreateDrive(_currentStrength);

        if (_useSlerpDrive)
        {
            _joint.slerpDrive = drive;
            return;
        }

        _joint.angularXDrive = drive;
        _joint.angularYZDrive = drive;
    }

    public float GetStrength()
    {
        return _currentStrength;
    }

    private void CacheInitialRotations()
    {
        _initialLocalRotation = transform.localRotation;
        _initialAnimatedLocalRotation = animatedBone != null
            ? animatedBone.localRotation
            : Quaternion.identity;
        _jointSpaceRotation = BuildJointSpace(_joint);
    }

    private void ConfigureJointDrive()
    {
        // PhysX ignores the SLERP drive when any angular degree of freedom
        // is locked. Knees and ankles intentionally lock their swing axes,
        // so drive those joints with the separate twist/swing drives instead.
        _useSlerpDrive =
            _joint.angularXMotion != ConfigurableJointMotion.Locked
            && _joint.angularYMotion != ConfigurableJointMotion.Locked
            && _joint.angularZMotion != ConfigurableJointMotion.Locked;

        _joint.rotationDriveMode = _useSlerpDrive
            ? RotationDriveMode.Slerp
            : RotationDriveMode.XYAndZ;
    }

    private void ConfigureRigidbodySolver()
    {
        if (_body == null)
            return;

        _body.solverIterations = Mathf.Max(
            _body.solverIterations,
            MinimumSolverIterations);
        _body.solverVelocityIterations = Mathf.Max(
            _body.solverVelocityIterations,
            MinimumSolverVelocityIterations);
        _body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void CalculateBaseDrive()
    {
        _baseSpring = springPerKg * strengthMultiplier;
        _baseDamper = 2f * dampingRatio * Mathf.Sqrt(_baseSpring);
    }

    private void InitializeTargetRotation()
    {
        _smoothedTargetRotation = animatedBone != null
            ? CalculateTargetRotation(animatedBone.localRotation)
            : Quaternion.identity;
    }

    private JointDrive CreateDrive(float strength)
    {
        return new JointDrive
        {
            positionSpring = _baseSpring * strength,
            positionDamper = _baseDamper * strength,
            maximumForce = Mathf.Infinity,
            useAcceleration = true
        };
    }

    private Quaternion CalculateTargetRotation(Quaternion animatedLocalRotation)
    {
        Quaternion rotationFromTargetToInitial;

        if (name.StartsWith("shin"))
        {
            // The imported knee hinge uses the opposite signed rotation from
            // its animated peer. Invert only its delta from the bind pose.
            rotationFromTargetToInitial =
                Quaternion.Inverse(_initialAnimatedLocalRotation)
                * animatedLocalRotation;
        }
        else
        {
            rotationFromTargetToInitial =
                Quaternion.Inverse(animatedLocalRotation)
                * _initialAnimatedLocalRotation;
        }

        return Quaternion.Inverse(_jointSpaceRotation)
             * rotationFromTargetToInitial
             * _jointSpaceRotation;
    }

    private void CacheCenterOfMassOffset()
    {
        if (_body == null)
            return;

        _centerOfMassOffset = Quaternion.Inverse(transform.rotation)
                            * (_body.worldCenterOfMass - transform.position);
    }

    private void FollowAnimatedCenterOfMass()
    {
        if (!followPosition || _body == null || _body.isKinematic)
            return;

        Vector3 targetCenterOfMass = animatedBone.position
                                   + animatedBone.rotation * _centerOfMassOffset;

        if (!_hasPreviousTargetCenterOfMass)
        {
            _previousTargetCenterOfMass = targetCenterOfMass;
            _hasPreviousTargetCenterOfMass = true;
        }

        Vector3 targetVelocity =
            (targetCenterOfMass - _previousTargetCenterOfMass)
            / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        targetVelocity = Vector3.ClampMagnitude(
            targetVelocity,
            maximumTargetSpeed);

        Vector3 bodyCenterOfMass = _proxyMode
            ? transform.position + transform.rotation * _centerOfMassOffset
            : _body.worldCenterOfMass;
        Vector3 positionError = targetCenterOfMass - bodyCenterOfMass;

        Vector3 relativeVelocity;
        if (_proxyMode)
        {
            if (!_hasPreviousPositionError)
            {
                _previousPositionError = positionError;
                _hasPreviousPositionError = true;
            }

            relativeVelocity = Vector3.ClampMagnitude(
                (positionError - _previousPositionError)
                / Mathf.Max(Time.fixedDeltaTime, 0.0001f),
                maximumTargetSpeed);
            _previousPositionError = positionError;
        }
        else
        {
            relativeVelocity = targetVelocity - _body.linearVelocity;
        }

        float positionGain = IsLowerBodyBone()
            ? LowerBodyPositionGain
            : 1f;
        float effectiveSpring = positionSpring * positionGain;
        float damper = 2f * positionDampingRatio * Mathf.Sqrt(effectiveSpring);
        Vector3 acceleration = effectiveSpring * positionError
                               + damper * relativeVelocity
                               - Physics.gravity;
        acceleration *= _currentStrength;
        acceleration = Vector3.ClampMagnitude(
            acceleration,
            maximumAcceleration * positionGain * _currentStrength);

        _body.AddForce(acceleration, ForceMode.Acceleration);
        _previousTargetCenterOfMass = targetCenterOfMass;
    }

    private void ConfigureJointLimits()
    {
        if (name.StartsWith("forearm"))
        {
            ConfigureForearmHinge();
            return;
        }

        if (name.StartsWith("thigh"))
        {
            SetAngularMotions(
                ConfigurableJointMotion.Limited,
                ConfigurableJointMotion.Limited,
                ConfigurableJointMotion.Locked);

            // X lifts the leg forward/backward. Keep only a modest amount of
            // lateral hip movement and lock axial twist so the whole leg
            // cannot escape into a V shape under the position follower.
            SetAngularLimits(-110f, 110f, 20f, 0f);
            return;
        }

        if (name.StartsWith("shin"))
        {
            // Keep the knee as a hinge so it cannot become an animal-like
            // backwards knee, but allow the full bend used by the IK step.
            SetAngularMotions(
                ConfigurableJointMotion.Limited,
                ConfigurableJointMotion.Locked,
                ConfigurableJointMotion.Locked);
            SetAngularLimits(-140f, 5f, 0f, 0f);
            return;
        }

        if (!name.StartsWith("foot"))
            return;

        // This rig's ankle hinge is the joint X axis. Keep roll and yaw
        // locked so the foot can only flex forward and backward.
        SetAngularMotions(
            ConfigurableJointMotion.Limited,
            ConfigurableJointMotion.Locked,
            ConfigurableJointMotion.Locked);
        SetAngularLimits(-60f, 60f, 0f, 0f);
    }

    private void ConfigureForearmHinge()
    {
        // Mirror the elbow hinge around the character's vertical axis. With a
        // positive bend on both sides this makes each hand travel forward and
        // then inward, instead of sending the left elbow to the outside.
        bool isLeftArm = name.EndsWith(".L");
        Vector3 worldHingeAxis = isLeftArm
            ? transform.root.up
            : -transform.root.up;
        Vector3 localHingeAxis = transform
            .InverseTransformDirection(worldHingeAxis)
            .normalized;

        _joint.axis = localHingeAxis;

        // Keep the secondary axis perpendicular after changing the primary
        // axis so BuildJointSpace always receives a stable orthonormal basis.
        Vector3 secondaryAxis = Vector3.ProjectOnPlane(
            _joint.secondaryAxis,
            localHingeAxis).normalized;
        if (secondaryAxis.sqrMagnitude < MinimumAxisMagnitudeSqr)
            secondaryAxis = Vector3.Cross(localHingeAxis, Vector3.forward).normalized;
        if (secondaryAxis.sqrMagnitude < MinimumAxisMagnitudeSqr)
            secondaryAxis = Vector3.Cross(localHingeAxis, Vector3.up).normalized;
        _joint.secondaryAxis = secondaryAxis;

        SetAngularMotions(
            ConfigurableJointMotion.Limited,
            ConfigurableJointMotion.Locked,
            ConfigurableJointMotion.Locked);
        SetAngularLimits(0f, 120f, 0f, 0f);
    }

    private void SetAngularMotions(
        ConfigurableJointMotion x,
        ConfigurableJointMotion y,
        ConfigurableJointMotion z)
    {
        _joint.angularXMotion = x;
        _joint.angularYMotion = y;
        _joint.angularZMotion = z;
    }

    private void SetAngularLimits(
        float lowX,
        float highX,
        float y,
        float z)
    {
        SoftJointLimit lowXLimit = _joint.lowAngularXLimit;
        lowXLimit.limit = lowX;
        _joint.lowAngularXLimit = lowXLimit;

        SoftJointLimit highXLimit = _joint.highAngularXLimit;
        highXLimit.limit = highX;
        _joint.highAngularXLimit = highXLimit;

        SoftJointLimit yLimit = _joint.angularYLimit;
        yLimit.limit = y;
        _joint.angularYLimit = yLimit;

        SoftJointLimit zLimit = _joint.angularZLimit;
        zLimit.limit = z;
        _joint.angularZLimit = zLimit;
    }

    private bool IsLowerBodyBone()
    {
        return name.StartsWith("thigh")
            || name.StartsWith("shin")
            || name.StartsWith("foot");
    }

    private static Quaternion BuildJointSpace(ConfigurableJoint joint)
    {
        Vector3 right = joint.axis.normalized;
        Vector3 forward = Vector3.Cross(right, joint.secondaryAxis).normalized;

        if (forward.sqrMagnitude < MinimumAxisMagnitudeSqr)
            forward = Vector3.Cross(right, Vector3.up).normalized;

        if (forward.sqrMagnitude < MinimumAxisMagnitudeSqr)
            forward = Vector3.Cross(right, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(forward, right).normalized;
        return Quaternion.LookRotation(forward, up);
    }
}
