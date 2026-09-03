using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LowerBodyDriver : MonoBehaviour
{
    [Header("Rigs")]
    [SerializeField] Animator animator;
    [SerializeField] Transform animatedHips;   // Skeleton/root
    [SerializeField] Rigidbody physicsHips;    // Visual/root

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float turnSpeed = 12f;
    [SerializeField] float gravity = -20f;

    CharacterController cc;
    Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.Fixed;

        if (physicsHips != null)
        {
            physicsHips.isKinematic = true;
            physicsHips.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void FixedUpdate()
    {
        Move();
        FollowHips();
    }

    void Move()
    {
        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 world = transform.TransformDirection(input) * moveSpeed;

        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.fixedDeltaTime;

        cc.Move((world + Vector3.up * velocity.y) * Time.fixedDeltaTime);

        if (animator != null)
            animator.SetFloat("Speed", input.magnitude, 0.1f, Time.fixedDeltaTime);
    }

    void FollowHips()
    {
        if (animatedHips == null || physicsHips == null) return;

        // MovePosition을 써야 자식 조인트에 속도가 전달됨
        physicsHips.MovePosition(animatedHips.position);
        physicsHips.MoveRotation(animatedHips.rotation);
    }
}