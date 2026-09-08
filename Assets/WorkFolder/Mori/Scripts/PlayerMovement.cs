using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovementInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float maxRunSpeed = 10f;
    [SerializeField, Min(0f)] private float forwardAcceleration = 3f;

    [Header("Camera Facing")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [SerializeField] private float modelYawOffset;

    private Rigidbody _rigidbody;
    private PlayerMovementInput _input;
    private float _currentForwardSpeed;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerMovementInput>();
        _currentForwardSpeed = walkSpeed;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void FixedUpdate()
    {
        Quaternion cameraYawRotation = RotateTowardCamera();

        Vector2 moveInput = _input.MoveDirection;
        Vector3 localDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (localDirection.sqrMagnitude == 0f)
        {
            _currentForwardSpeed = walkSpeed;
            StopHorizontalMovement();
            return;
        }

        float speed;

        if (_input.IsForwardOnly)
        {
            _currentForwardSpeed = Mathf.MoveTowards(
                _currentForwardSpeed,
                Mathf.Max(walkSpeed, maxRunSpeed),
                forwardAcceleration * Time.fixedDeltaTime);
            speed = _currentForwardSpeed;
        }
        else
        {
            // A, S, D가 하나라도 눌리면 누적된 달리기 속도를 즉시 끊는다.
            _currentForwardSpeed = walkSpeed;
            speed = walkSpeed;
        }

        // 이동 방향도 카메라의 수평 방향을 기준으로 계산한다.
        Vector3 worldDirection = cameraYawRotation * localDirection.normalized;
        Vector3 horizontalVelocity = worldDirection * speed;

        _rigidbody.linearVelocity = new Vector3(
            horizontalVelocity.x,
            _rigidbody.linearVelocity.y,
            horizontalVelocity.z);
    }

    private Quaternion RotateTowardCamera()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return _rigidbody.rotation * Quaternion.Euler(0f, -modelYawOffset, 0f);
            }

            cameraTransform = mainCamera.transform;
        }

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            return _rigidbody.rotation * Quaternion.Euler(0f, -modelYawOffset, 0f);
        }

        Quaternion cameraYawRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
        Quaternion targetRotation = cameraYawRotation * Quaternion.Euler(0f, modelYawOffset, 0f);
        Quaternion nextRotation = Quaternion.RotateTowards(
            _rigidbody.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime);

        _rigidbody.MoveRotation(nextRotation);
        return cameraYawRotation;
    }

    private void StopHorizontalMovement()
    {
        _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
    }
}
