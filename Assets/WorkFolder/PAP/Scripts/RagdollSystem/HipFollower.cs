using UnityEngine;

public class HipFollower : MonoBehaviour
{
    [SerializeField] Transform animatedHips;   // Skeleton/root
    [SerializeField] Rigidbody physicsHips;    // Visual/root
    [SerializeField] float teleportThreshold = 0.5f;
    void Awake()
    {
        if (physicsHips != null)
        {
            physicsHips.isKinematic = true;
            physicsHips.interpolation = RigidbodyInterpolation.None;
        }
    }

    void FixedUpdate()
    {
        FollowHips();
    }

    void FollowHips()
    {
        Vector3 target = animatedHips.position;

        if ((target - physicsHips.position).sqrMagnitude
            > teleportThreshold * teleportThreshold)
        {
            physicsHips.position = target;
            physicsHips.rotation = animatedHips.rotation;
        }
        else
        {
            physicsHips.MovePosition(target);
            physicsHips.MoveRotation(animatedHips.rotation);
        }
    }
}