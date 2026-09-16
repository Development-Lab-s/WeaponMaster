using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class PuppetJoint : MonoBehaviour
{
    [SerializeField] Transform animatedBone;

    [Header("Strength")]
    [SerializeField] float strengthMultiplier = 1f;
    [SerializeField] float dampingRatio = 0.6f;
    [SerializeField] float springPerKg = 900f;
    [SerializeField] private float maxTorque = 5000f;

    [Header("Smoothing")]
    [Range(0.05f, 1f)]
    [SerializeField] float followRate = 0.35f;

    ConfigurableJoint joint;
    Quaternion startLocalRotation;

    // targetRotation은 로컬 공간이 아니라 axis/secondaryAxis가 정의하는
    // 조인트 공간에서 해석된다. 그 변환을 미리 구해둔다.
    Quaternion worldToJointSpace;

    float baseSpring, baseDamper;
    float currentStrength = 1f;
    bool initialized;
    Quaternion smoothed;

    public Transform AnimatedBone => animatedBone;

    void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
        startLocalRotation = transform.localRotation;
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        worldToJointSpace = BuildJointSpace(joint.axis, joint.secondaryAxis);

        float load = 0f;
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            load += rb.mass;
        if (load <= 0f) load = 1f;

        baseSpring = load * springPerKg * strengthMultiplier;
        baseDamper = 2f * dampingRatio * Mathf.Sqrt(baseSpring * load);
        SetStrength(1f);
    }

    static Quaternion BuildJointSpace(Vector3 axis, Vector3 secondaryAxis)
    {
        Vector3 right = axis.normalized;
        Vector3 forward = Vector3.Cross(right, secondaryAxis.normalized);

        // 두 축이 평행하면 외적이 0이 된다. 임의의 직교축으로 대체.
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.Cross(right, Mathf.Abs(right.z) < 0.9f ? Vector3.forward : Vector3.up);

        forward.Normalize();
        Vector3 up = Vector3.Cross(forward, right).normalized;
        return Quaternion.LookRotation(forward, up);
    }

    public void SetStrength(float t)
    {
        currentStrength = Mathf.Clamp01(t);
        joint.slerpDrive = new JointDrive
        {
            positionSpring = baseSpring * currentStrength,
            positionDamper = baseDamper * currentStrength,
            maximumForce   = maxTorque
        };
    }

    public float GetStrength() => currentStrength;

    void FixedUpdate()
    {
        if (animatedBone == null) return;

        // 로컬 공간에서의 목표 → 조인트 공간으로 변환
        Quaternion local = Quaternion.Inverse(animatedBone.localRotation) * startLocalRotation;
        Quaternion desired = Quaternion.Inverse(worldToJointSpace) * local * worldToJointSpace;

        // 첫 프레임에 identity에서 끌려오지 않도록 목표값으로 초기화
        if (!initialized) { smoothed = desired; initialized = true; }

        smoothed = Quaternion.Slerp(smoothed, desired, followRate);
        joint.targetRotation = smoothed;
    }
}
