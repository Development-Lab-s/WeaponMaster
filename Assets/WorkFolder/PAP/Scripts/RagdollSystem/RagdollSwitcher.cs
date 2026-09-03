using UnityEngine;

public class RagdollSwitch : MonoBehaviour
{
    [Header("Rigs")]
    [SerializeField] Transform skeletonRoot;
    [SerializeField] Transform visualRoot;

    [Header("Components to toggle")]
    [SerializeField] Animator animator;
    [SerializeField] CharacterController controller;
    [SerializeField] BoneCopier boneCopier;
    [SerializeField] LowerBodyDriver lowerDriver;

    Rigidbody[] bodies;
    ConfigurableJoint[] joints;
    PuppetJoint[] puppets;
    bool isRagdoll;

    public bool IsRagdoll => isRagdoll;

    void Awake()
    {
        bodies  = visualRoot.GetComponentsInChildren<Rigidbody>(true);
        joints  = visualRoot.GetComponentsInChildren<ConfigurableJoint>(true);
        puppets = visualRoot.GetComponentsInChildren<PuppetJoint>(true);

        ApplyState(false);
    }

    public void SetRagdoll(bool on)
    {
        if (isRagdoll == on) return;

        if (on) CopyPoseFromSkeleton();
        ApplyState(on);
        isRagdoll = on;
    }

    void ApplyState(bool on)
    {
        foreach (var rb in bodies)
        {
            rb.isKinematic = !on;
            rb.interpolation = on
                ? RigidbodyInterpolation.Interpolate
                : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = on
                ? CollisionDetectionMode.ContinuousDynamic
                : CollisionDetectionMode.Discrete;
        }

        // 래그돌일 땐 관절이 늘어나지 않게 preprocessing 복구
        foreach (var j in joints)
            j.enablePreprocessing = on;

        // 액티브 드라이브 해제 → 완전히 축 늘어짐
        foreach (var p in puppets)
            p.enabled = !on;

        if (animator    != null) animator.enabled   = !on;
        if (boneCopier  != null) boneCopier.enabled = !on;
        if (lowerDriver != null) lowerDriver.enabled = !on;
        if (controller  != null) controller.enabled = !on;
    }

    // 전환 순간 자세가 튀지 않게 스켈레톤 포즈를 그대로 이어받음
    void CopyPoseFromSkeleton()
    {
        var s = skeletonRoot.GetComponentsInChildren<Transform>();
        var v = visualRoot.GetComponentsInChildren<Transform>();

        int n = Mathf.Min(s.Length, v.Length);
        for (int i = 0; i < n; i++)
        {
            if (s[i].name != v[i].name) continue;
            v[i].localPosition = s[i].localPosition;
            v[i].localRotation = s[i].localRotation;
        }
    }

    public void Die(Vector3 direction, float force, Rigidbody hitPart)
    {
        SetRagdoll(true);

        if (hitPart != null)
            hitPart.AddForce(direction.normalized * force, ForceMode.Impulse);
    }
}