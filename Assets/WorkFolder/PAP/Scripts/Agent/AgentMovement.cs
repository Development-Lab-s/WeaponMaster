using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Agent
{
    public interface IAgentMovement
    {
        void SetMovementDirection(Vector2 dir);
    }

    public class AgentMovement : NetworkModule, IAgentMovement
    {
        [SerializeField] private float moveSpeed = 5;
        [SerializeField] private float accelation = 0.3f;
        
        public Vector3 MoveDirection { get; private set; }
        public ICameraController CameraController { get; private set; }
        
        private Rigidbody _rigidbody;
        private Vector3 _currentDirection;

        public override void Initialize(NetworkOwner owner)
        {
            base.Initialize(owner);
            _rigidbody = owner.GetComponent<Rigidbody>();
            CameraController = owner.GetModule<ICameraController>();
        }

        public void SetMovementDirection(Vector2 dir)
        {
            MoveDirection = new Vector3(dir.x, 0, dir.y).normalized;
        }

        
        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false) return;
            
            Vector3 dir = transform.TransformDirection(MoveDirection);
            dir.y = _rigidbody.linearVelocity.y;
            _currentDirection = Vector3.Lerp(_currentDirection, dir * moveSpeed, accelation);
            _rigidbody.linearVelocity = new Vector3(_currentDirection.x, _rigidbody.linearVelocity.y, _currentDirection.z);
            //움직임
            _rigidbody.rotation = Quaternion.Euler(0, CameraController.EularAngle.y, 0);
        }
        
        
    }
}