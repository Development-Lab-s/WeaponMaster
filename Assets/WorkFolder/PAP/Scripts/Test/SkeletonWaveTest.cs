using UnityEngine;

public class SkeletonWaveTest : MonoBehaviour
{
    [SerializeField] Transform bone;      // Skeleton/upper_arm.L
    [SerializeField] Vector3 axis = Vector3.right;
    [SerializeField] float amplitude = 45f;
    [SerializeField] float speed = 1.5f;

    Quaternion start;

    void Awake() => start = bone.localRotation;

    void Update()
    {
        float a = Mathf.Sin(Time.time * speed) * amplitude;
        bone.localRotation = start * Quaternion.AngleAxis(a, axis);
    }
}