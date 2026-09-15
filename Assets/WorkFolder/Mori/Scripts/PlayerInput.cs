using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementInput : MonoBehaviour
{
    public Vector2 MoveDirection { get; private set; }
    public bool HasForwardInput { get; private set; }
    public bool IsSprintPressed { get; private set; }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            ClearInput();
            return;
        }

        bool wPressed = keyboard.wKey.isPressed;
        bool aPressed = keyboard.aKey.isPressed;
        bool sPressed = keyboard.sKey.isPressed;
        bool dPressed = keyboard.dKey.isPressed;

        MoveDirection = new Vector2(
            (dPressed ? 1f : 0f) - (aPressed ? 1f : 0f),
            (wPressed ? 1f : 0f) - (sPressed ? 1f : 0f)).normalized;

        HasForwardInput = wPressed && !sPressed;
        IsSprintPressed = keyboard.leftShiftKey.isPressed ||
                          keyboard.rightShiftKey.isPressed;
    }

    private void OnDisable()
    {
        ClearInput();
    }

    private void ClearInput()
    {
        MoveDirection = Vector2.zero;
        HasForwardInput = false;
        IsSprintPressed = false;
    }
}
