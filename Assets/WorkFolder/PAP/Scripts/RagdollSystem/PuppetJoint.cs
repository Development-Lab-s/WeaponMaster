using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class PuppetJoint : MonoBehaviour
{
    [SerializeField] Transform animatedBone;

    [Header("Strength")]
    [SerializeField] float strengthMultiplier = 1f;
    [SerializeField] float dampingRatio = 0.6f;
    [SerializeField] float springPerKg = 900f;

    ConfigurableJoint joint;
    Quaternion startLocalRotation;
    float baseSpring, baseDamper;
    float currentStrength = 1f;

    public Transform AnimatedBone => animatedBone;

    void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
        startLocalRotation = transform.localRotation;
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        float load = 0f;
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            load += rb.mass;
        if (load <= 0f) load = 1f;

        baseSpring = load * springPerKg * strengthMultiplier;
        baseDamper = 2f * dampingRatio * Mathf.Sqrt(baseSpring * load);

        SetStrength(1f);
    }

    public void SetStrength(float t)
    {
        currentStrength = Mathf.Clamp01(t);
        joint.slerpDrive = new JointDrive
        {
            positionSpring = baseSpring * currentStrength,
            positionDamper = baseDamper * currentStrength,
            maximumForce   = Mathf.Infinity
        };
    }

    public float GetStrength() => currentStrength;

    void FixedUpdate()
    {
        if (animatedBone == null) return;
        joint.targetRotation =
            Quaternion.Inverse(animatedBone.localRotation) * startLocalRotation;
    }
}