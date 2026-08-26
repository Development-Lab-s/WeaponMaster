using System;
using DevLib.ModuleSystem;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class PlayerController : NetworkOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        public IAgentMovement AgentMover { get; private set; }
        
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            AgentMover = GetModule<IAgentMovement>();
        }

        public override void Spawned()
        {
            if (!HasStateAuthority) return;
                CinemachineCore.GetVirtualCamera(0).Follow = transform;
        }

        private void Update()
        {
            if (!HasStateAuthority) return;
            AgentMover.SetMovementDirection(playerInput.MoveDirection);
        }
    }
}