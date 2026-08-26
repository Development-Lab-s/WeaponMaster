using UnityEngine;
using UnityEngine.InputSystem;

public class MouseCameraLook : MonoBehaviour
{
    [SerializeField] private MouseSensitivitySetting sensitivitySetting;

    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private float mouseScale = 0.1f;

    private float rotationX;
    private float rotationY;

    private void Start()
    {
        Vector3 currentRotation = transform.localEulerAngles;

        rotationX = currentRotation.x;
        rotationY = currentRotation.y;

        if (rotationX > 180f)
        {
            rotationX -= 360f;
        }
    }

    private void Update()
    {
        if (sensitivitySetting == null || Mouse.current == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float sensitivity = sensitivitySetting.Sensitivity;

        float mouseX = mouseDelta.x * sensitivity * mouseScale;
        float mouseY = mouseDelta.y * sensitivity * mouseScale;

        rotationY += mouseX;
        rotationX -= mouseY;

        rotationX = Mathf.Clamp(
            rotationX,
            minVerticalAngle,
            maxVerticalAngle
        );

        transform.localRotation = Quaternion.Euler(
            rotationX,
            rotationY,
            0f
        );
    }
}
