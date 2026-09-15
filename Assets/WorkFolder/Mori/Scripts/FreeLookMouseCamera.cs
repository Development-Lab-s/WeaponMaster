using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class FreeLookMouseCamera : MonoBehaviour
{
    [Header("Mouse Look")]
    [SerializeField, Min(0f)] private float horizontalSensitivity = 0.15f;
    [SerializeField, Min(0f)] private float verticalSensitivity = 0.15f;
    [SerializeField] private bool lockCursor = true;

    private CinemachineOrbitalFollow _orbitalFollow;
    private CinemachineInputAxisController _inputAxisController;
    private bool _inputAxisControllerWasEnabled;

    private void Awake()
    {
        _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        _inputAxisController = GetComponent<CinemachineInputAxisController>();

        // 이 스크립트와 Cinemachine 입력이 동시에 축을 조작하지 않도록 막는다
        if (_inputAxisController != null)
        {
            _inputAxisControllerWasEnabled = _inputAxisController.enabled;
        }
    }

    private void OnEnable()
    {
        if (_inputAxisController != null)
        {
            _inputAxisController.enabled = false;
        }

        SetCursorState(lockCursor);
    }

    private void OnDisable()
    {
        if (_inputAxisController != null)
        {
            _inputAxisController.enabled = _inputAxisControllerWasEnabled;
        }

        SetCursorState(false);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !Application.isFocused)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        _orbitalFollow.HorizontalAxis.Value = _orbitalFollow.HorizontalAxis.ClampValue(
            _orbitalFollow.HorizontalAxis.Value + mouseDelta.x * horizontalSensitivity);

        _orbitalFollow.VerticalAxis.Value = _orbitalFollow.VerticalAxis.ClampValue(
            _orbitalFollow.VerticalAxis.Value - mouseDelta.y * verticalSensitivity);
    }

    private static void SetCursorState(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
}
