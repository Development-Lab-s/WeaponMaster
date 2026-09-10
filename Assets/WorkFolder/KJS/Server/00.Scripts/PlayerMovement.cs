using System;
using _00.Scripts;
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private Vector3 _velocity;
    private bool _jumpPressed;

    public Camera Camera;
    private CharacterController _controller;

    public float playerSpeed = 2f;

    public float jumpForce = 5f;
    public float gravityValue = -9.81f;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }
    }
    
    
    public override void Spawned()
    {
        Debug.Log($"Player Spawned: {Object.Id}");

        if (HasStateAuthority)
        {
            Debug.Log("I have State Authority");

            Camera = Camera.main;

            Debug.Log($"Main Camera: {Camera}");

            Camera.GetComponent<FisrtPersonCamera>().target = transform;
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (HasStateAuthority == false) return;

        if (_controller.isGrounded)
        {
            _velocity = new Vector3(0, -1, 0);
        }

        Quaternion cemeraRotationY = Quaternion.Euler(0,Camera.transform.rotation.eulerAngles.y,0);
        Vector3 move = cemeraRotationY * new Vector3(Input.GetAxis("Horizontal"), 0,
            Input.GetAxis("Vertical") * Runner.DeltaTime * playerSpeed);

        _velocity.y += gravityValue * Runner.DeltaTime;

        if (_jumpPressed && _controller.isGrounded)
        {
            _velocity.y += jumpForce;
        }
        
        _controller.Move(move + _velocity * Runner.DeltaTime);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
        }
        
        _jumpPressed = false;
    }
}
