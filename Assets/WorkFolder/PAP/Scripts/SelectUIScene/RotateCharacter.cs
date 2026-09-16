using System;
using UnityEngine;
using UnityEngine.EventSystems;
using WorkFolder.PAP.Scripts.Agent.Player;

namespace WorkFolder.PAP.Scripts.SelectUIScene
{
    public class RotateCharacter : MonoBehaviour
    {
        [SerializeField] private Transform targetTrm;
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private float strength = 5f;
        [SerializeField] private float speed = -5f;
        [SerializeField] private float dragSpeed = 20f;
        

        private bool _isActive = false;
        private bool _rotateEnabled = true;
        private Vector3 _rot;
        
        private void Awake()
        {
            playerInput.OnAttackKeyPressed += HandleMouseButtonDown;
        }

        private void Start()
        {
            InventoryDragController.Instance.SetRotateChar(this);
        }

        private void OnDestroy()
        {
            playerInput.OnAttackKeyPressed -= HandleMouseButtonDown;
        }

        private void HandleMouseButtonDown(bool obj)
        {
            _isActive = obj;
        }

        public void SetRotateEnabled(bool value)
        {
            _rotateEnabled = value;
        }

        private void Update()
        {
            if (!_rotateEnabled) return;
            targetTrm.rotation = Quaternion.Slerp(targetTrm.rotation, Quaternion.Euler(_rot), Time.deltaTime * strength);
            if (_isActive)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    
                    _rot += new Vector3(0, -playerInput.AimDirection.x * dragSpeed * Time.deltaTime, 0);
                }
            }
            _rot += new Vector3(0, speed * Time.deltaTime, 0);
        }
    }
}