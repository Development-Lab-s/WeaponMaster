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
            // 루트/본과 보간 모드를 맞춘다. Interpolate면 힙만 한 물리 스텝 과거를 그려
            // 이동 중 리그 전체가 스켈레톤보다 뒤처진다.
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