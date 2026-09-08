using UnityEngine;

public sealed class RagdollPhysics : MonoBehaviour
{
    public Rigidbody pelvis;
    public Transform animatedPelvis;

    void Awake()
    {
        // The movement capsule and ragdoll belong to the same character.
        // Internal overlaps must not propel the character root.
        var colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            for (int j = i + 1; j < colliders.Length; j++)
                Physics.IgnoreCollision(colliders[i], colliders[j]);
        if (pelvis != null) pelvis.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (pelvis == null || animatedPelvis == null) return;
        pelvis.MovePosition(animatedPelvis.position);
        pelvis.MoveRotation(animatedPelvis.rotation);
    }
}
