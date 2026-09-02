using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PlayerHipCamera : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private Transform playerRoot;

    [Header("한 바퀴 회전")]
    [SerializeField] private Transform orbitCenter;
    [SerializeField] private Transform orbitLookTarget;
    [SerializeField] private float orbitDuration = 2.5f;

    [Header("엉덩이 카메라")]
    [SerializeField] private Transform hipCameraPoint;
    [SerializeField] private Transform hipLookTarget;
    [SerializeField] private float hipMoveDuration = 1f;

    [Header("복귀")]
    [SerializeField] private float returnDuration = 1f;
    
    [Header("엉덩이 화면 이벤트")]
    [SerializeField] private UnityEvent onHipViewEntered;
    [SerializeField] private UnityEvent onHipViewExited;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Vector3 orbitStartLocalOffset;

    private Vector3 phaseStartLocalPosition;
    private Quaternion phaseStartLocalRotation;

    private float phaseTime;

    private CameraState cameraState = CameraState.Normal;

    public bool CanSelectHip =>
        cameraState == CameraState.Hip;

    private enum CameraState
    {
        Normal,
        Orbiting,
        MovingToHip,
        Hip,
        Returning
    }

    private void Update()
    {
        // ESC 입력 처리
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            switch (cameraState)
            {
                case CameraState.Normal:
                    StartOrbit(true);
                    break;

                case CameraState.Hip:
                    StartReturning();
                    break;

                case CameraState.Orbiting:
                case CameraState.MovingToHip:
                case CameraState.Returning:
                    // 카메라 이동 중에는 ESC 무시
                    break;
            }

            return;
        }

        // 엉덩이 화면이 아니면 클릭 처리하지 않음
        if (cameraState != CameraState.Hip ||
            Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // 마우스가 UI 위에 있는지 확인
        bool isPointerOverUI =
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(
                Mouse.current.deviceId
            );

        // 버튼 위를 클릭했다면 카메라 복귀를 실행하지 않음
        if (isPointerOverUI)
        {
            return;
        }

        // UI가 없는 빈 곳을 클릭하면 원래 카메라로 복귀
        StartReturning();
    }

    private void LateUpdate()
    {
        float deltaTime = Time.unscaledDeltaTime;

        switch (cameraState)
        {
            case CameraState.Orbiting:
                UpdateOrbit(deltaTime);
                break;

            case CameraState.MovingToHip:
                UpdateMoveToHip(deltaTime);
                break;

            case CameraState.Hip:
                UpdateHipFollow();
                break;

            case CameraState.Returning:
                UpdateReturning(deltaTime);
                break;
        }
    }

    private void StartOrbit(bool saveOriginalPosition)
    {
        if (!CheckReferences())
        {
            return;
        }

        if (saveOriginalPosition)
        {
            originalParent = transform.parent;

            // 플레이어 기준으로 기존 카메라 위치 저장
            originalLocalPosition =
                playerRoot.InverseTransformPoint(transform.position);

            originalLocalRotation =
                Quaternion.Inverse(playerRoot.rotation)
                * transform.rotation;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 부모의 이상한 회전과 스케일 영향 제거
        transform.SetParent(null, true);

        // OrbitCenter 기준 현재 카메라 위치
        orbitStartLocalOffset =
            orbitCenter.InverseTransformPoint(transform.position);

        phaseTime = 0f;
        cameraState = CameraState.Orbiting;
    }

    private void UpdateOrbit(float deltaTime)
    {
        phaseTime += deltaTime;

        float progress = Mathf.Clamp01(
            phaseTime / orbitDuration
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        // 정확히 한 바퀴
        float angle = 360f * smoothProgress;

        Vector3 rotatedLocalOffset =
            Quaternion.AngleAxis(angle, Vector3.up)
            * orbitStartLocalOffset;

        // OrbitCenter가 플레이어를 따라가므로
        // 플레이어가 이동해도 카메라가 함께 이동
        transform.position =
            orbitCenter.TransformPoint(rotatedLocalOffset);

        LookAtTarget(orbitLookTarget);

        if (progress >= 1f)
        {
            StartMovingToHip();
        }
    }

    private void StartMovingToHip()
    {
        phaseStartLocalPosition =
            playerRoot.InverseTransformPoint(transform.position);

        phaseStartLocalRotation =
            Quaternion.Inverse(playerRoot.rotation)
            * transform.rotation;

        phaseTime = 0f;
        cameraState = CameraState.MovingToHip;
    }

    private void UpdateMoveToHip(float deltaTime)
    {
        phaseTime += deltaTime;

        float progress = Mathf.Clamp01(
            phaseTime / hipMoveDuration
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        Vector3 hipLocalPosition =
            playerRoot.InverseTransformPoint(
                hipCameraPoint.position
            );

        Quaternion hipWorldRotation =
            GetLookRotation(
                hipCameraPoint.position,
                hipLookTarget.position
            );

        Quaternion hipLocalRotation =
            Quaternion.Inverse(playerRoot.rotation)
            * hipWorldRotation;

        Vector3 currentLocalPosition = Vector3.Lerp(
            phaseStartLocalPosition,
            hipLocalPosition,
            smoothProgress
        );

        Quaternion currentLocalRotation = Quaternion.Slerp(
            phaseStartLocalRotation,
            hipLocalRotation,
            smoothProgress
        );

        transform.position =
            playerRoot.TransformPoint(currentLocalPosition);

        transform.rotation =
            playerRoot.rotation * currentLocalRotation;

        if (progress >= 1f)
        {
            // 최종 위치를 정확하게 맞춤
            transform.position = hipCameraPoint.position;

            transform.rotation = GetLookRotation(
                hipCameraPoint.position,
                hipLookTarget.position
            );

            cameraState = CameraState.Hip;

            Debug.Log("엉덩이 화면 진입 이벤트 실행");

            onHipViewEntered?.Invoke();
        }
        
    }

    private void UpdateHipFollow()
    {
        // 엉덩이 화면에서는 플레이어를 정확히 따라감
        transform.position = hipCameraPoint.position;

        transform.rotation = GetLookRotation(
            hipCameraPoint.position,
            hipLookTarget.position
        );
    }

    private void StartReturning()
    {
        if (cameraState != CameraState.Hip)
        {
            return;
        }

        onHipViewExited?.Invoke();

        phaseStartLocalPosition =
            playerRoot.InverseTransformPoint(transform.position);

        phaseStartLocalRotation =
            Quaternion.Inverse(playerRoot.rotation)
            * transform.rotation;

        phaseTime = 0f;
        cameraState = CameraState.Returning;
    }

    private void UpdateReturning(float deltaTime)
    {
        phaseTime += deltaTime;

        float progress = Mathf.Clamp01(
            phaseTime / returnDuration
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        Vector3 currentLocalPosition = Vector3.Lerp(
            phaseStartLocalPosition,
            originalLocalPosition,
            smoothProgress
        );

        Quaternion currentLocalRotation = Quaternion.Slerp(
            phaseStartLocalRotation,
            originalLocalRotation,
            smoothProgress
        );

        transform.position =
            playerRoot.TransformPoint(currentLocalPosition);

        transform.rotation =
            playerRoot.rotation * currentLocalRotation;
        
        if (progress < 1f)
        {
            return;
        }

// 플레이어가 이동한 상태를 기준으로 원래 위치 복원
        transform.position =
            playerRoot.TransformPoint(originalLocalPosition);

        transform.rotation =
            playerRoot.rotation * originalLocalRotation;

        transform.SetParent(originalParent, true);

        cameraState = CameraState.Normal;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
// 커서를 계속 표시
    }

    private void LookAtTarget(Transform target)
    {
        Vector3 direction =
            target.position - transform.position;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            direction.normalized,
            playerRoot.up
        );
    }

    private Quaternion GetLookRotation(
        Vector3 cameraPosition,
        Vector3 targetPosition
    )
    {
        Vector3 direction =
            targetPosition - cameraPosition;

        if (direction.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(
            direction.normalized,
            playerRoot.up
        );
    }

    private bool CheckReferences()
    {
        if (playerRoot == null ||
            orbitCenter == null ||
            orbitLookTarget == null ||
            hipCameraPoint == null ||
            hipLookTarget == null)
        {
            Debug.LogError(
                "PlayerHipCamera의 Transform이 연결되지 않았습니다."
            );

            return false;
        }

        return true;
    }
}