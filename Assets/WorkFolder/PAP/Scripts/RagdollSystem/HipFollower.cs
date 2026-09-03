using UnityEngine;

public class HipFollower : MonoBehaviour
{
    [SerializeField] Transform animatedHips;   // Skeleton/root
    [SerializeField] Rigidbody physicsHips;    // Visual/root
    void Awake()
    {
        if (physicsHips != null)
        {
            physicsHips.isKinematic = true;
            physicsHips.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void FixedUpdate()
    {
        FollowHips();
    }

    void FollowHips()
    {
        physicsHips.MovePosition(animatedHips.position);
        physicsHips.MoveRotation(animatedHips.rotation);
    }
}