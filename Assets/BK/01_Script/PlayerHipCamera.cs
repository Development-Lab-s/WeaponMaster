using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHipCamera : MonoBehaviour
{
    [SerializeField] private Transform player;

    // 플레이어 기준 엉덩이 카메라 위치
    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0f, 0.9f, -1.2f);

    // 카메라가 바라볼 위치
    [SerializeField]
    private Vector3 lookOffset = new Vector3(0f, 0.9f, 0f);

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 8f;

    // 기존 카메라 회전 스크립트가 있다면 Inspector에서 연결
    //[SerializeField] private MonoBehaviour normalCameraController;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    
    private OnSettingAndOnGoTOMainMenuButton setting;

    private CameraState cameraState = CameraState.Normal;

    private enum CameraState
    {
        Normal,
        Hip,
        Returning
    }

    private void Awake()
    {
        setting = GetComponentInChildren<OnSettingAndOnGoTOMainMenuButton>();
    }

    private void Update()
    {
        if (Keyboard.current == null || player == null)
        {
            return;
        }

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }
       

        switch (cameraState)
        {
            case CameraState.Normal:
                SaveOriginalCameraPosition();

                /*if (normalCameraController != null)
                {
                    normalCameraController.enabled = false;
                }*/

                cameraState = CameraState.Hip;
                break;

            case CameraState.Hip:
                cameraState = CameraState.Returning;
                break;

            case CameraState.Returning:
                // 복귀 중 ESC를 누르면 다시 엉덩이 카메라로 이동
                cameraState = CameraState.Hip;
                break;
        }
        setting.OnOffButton();
    }
    
    private void LateUpdate()
    {
        if (player == null || cameraState == CameraState.Normal)
        {
            return;
        }

        Vector3 targetPosition;
        Quaternion targetRotation;

        if (cameraState == CameraState.Hip)
        {
            targetPosition = player.TransformPoint(cameraOffset);

            Vector3 lookTarget = player.TransformPoint(lookOffset);
            Vector3 direction = lookTarget - targetPosition;

            targetRotation = Quaternion.LookRotation(
                direction,
                Vector3.up
            );
        }
        else
        {
            // 저장해놓은 원래 카메라 위치로 복귀
            targetPosition = player.TransformPoint(
                originalLocalPosition
            );

            targetRotation =
                player.rotation * originalLocalRotation;
        }

        float positionLerp =
            1f - Mathf.Exp(-moveSpeed * Time.deltaTime);

        float rotationLerp =
            1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            positionLerp
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerp
        );

        if (cameraState == CameraState.Returning)
        {
            FinishReturning(targetPosition, targetRotation);
        }
    }

    private void SaveOriginalCameraPosition()
    {
        originalLocalPosition =
            player.InverseTransformPoint(transform.position);

        originalLocalRotation =
            Quaternion.Inverse(player.rotation) * transform.rotation;
    }

    private void FinishReturning(
        Vector3 targetPosition,
        Quaternion targetRotation
    )
    {
        float positionDifference = Vector3.Distance(
            transform.position,
            targetPosition
        );

        float rotationDifference = Quaternion.Angle(
            transform.rotation,
            targetRotation
        );

        if (positionDifference > 0.01f ||
            rotationDifference > 0.1f)
        {
            return;
        }

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        cameraState = CameraState.Normal;

        /*if (normalCameraController != null)
        {
            //normalCameraController.enabled = true;
        }*/
    }
}