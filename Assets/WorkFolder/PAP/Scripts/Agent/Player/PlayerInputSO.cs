using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    [CreateAssetMenu(fileName = "Player Input", menuName = "System/Player Input", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        private Controls _controls;
        public Vector2 MoveDirection { get; private set; }
        public Vector2 AimDirection { get; private set; }
        public Action<bool> OnEquipKeyPressed;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Enable();
        }

        private void OnDisable()
        {
            if (_controls != null)
            {
                _controls.Disable();
                _controls = null;
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveDirection = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            AimDirection = context.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
        }

        public void OnDash(InputAction.CallbackContext context)
        {
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
        }

        public void OnEquip(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnEquipKeyPressed?.Invoke(true);
            else if (context.canceled)
                OnEquipKeyPressed?.Invoke(false);
        }
    }
}