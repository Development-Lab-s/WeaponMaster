using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class StablePuppetJoint : MonoBehaviour
{
    [SerializeField] Transform animatedBone;

    [Header("Strength")]
    [SerializeField] float strengthMultiplier = 1f;
    [SerializeField] float dampingRatio = 0.6f;
    [SerializeField] float springPerKg = 900f;
    [SerializeField, Range(0f, 1f)] float followRate = 0.35f;

    ConfigurableJoint joint;
    Quaternion startLocalRotation;
    Quaternion jointSpace;
    float baseSpring, baseDamper;
    float currentStrength = 1f;

    public Transform AnimatedBone => animatedBone;
    Quaternion _smoothed;

    void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
        BypassLightweightClavicleBody();
        startLocalRotation = transform.localRotation;
        jointSpace = BuildJointSpace(joint);
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.solverIterations = Mathf.Max(body.solverIterations, 16);
            body.solverVelocityIterations = Mathf.Max(body.solverVelocityIterations, 4);
        }

        baseSpring = springPerKg * strengthMultiplier;
        baseDamper = 2f * dampingRatio * Mathf.Sqrt(baseSpring);
        _smoothed = animatedBone != null
            ? GetTargetRotation(animatedBone.localRotation)
            : Quaternion.identity;
        SetStrength(1f);
    }

    public void SetStrength(float t)
    {
        currentStrength = Mathf.Clamp01(t);
        joint.slerpDrive = new JointDrive
        {
            positionSpring = baseSpring * currentStrength,
            positionDamper = baseDamper * currentStrength,
            maximumForce = Mathf.Infinity,
            useAcceleration = true
        };
    }

    public float GetStrength() => currentStrength;

    void FixedUpdate()
    {
        if (animatedBone == null) return;

        Quaternion desired = GetTargetRotation(animatedBone.localRotation);
        _smoothed = Quaternion.Slerp(_smoothed, desired, Mathf.Clamp01(followRate));
        joint.targetRotation = _smoothed;
    }

    Quaternion GetTargetRotation(Quaternion targetLocalRotation)
    {
        return Quaternion.Inverse(jointSpace)
             * (Quaternion.Inverse(targetLocalRotation) * startLocalRotation)
             * jointSpace;
    }

    static Quaternion BuildJointSpace(ConfigurableJoint configurableJoint)
    {
        Vector3 right = configurableJoint.axis.normalized;
        Vector3 forward = Vector3.Cross(right, configurableJoint.secondaryAxis).normalized;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.Cross(right, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.Cross(right, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(forward, right).normalized;
        return Quaternion.LookRotation(forward, up);
    }

    void BypassLightweightClavicleBody()
    {
        if (!name.StartsWith("upper_arm") || joint.connectedBody == null)
            return;

        var clavicleJoint = joint.connectedBody.GetComponent<ConfigurableJoint>();
        if (clavicleJoint == null || clavicleJoint.connectedBody == null)
            return;

        Vector3 anchorWorld = transform.TransformPoint(joint.anchor);
        joint.connectedBody = clavicleJoint.connectedBody;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(anchorWorld);
    }
}
