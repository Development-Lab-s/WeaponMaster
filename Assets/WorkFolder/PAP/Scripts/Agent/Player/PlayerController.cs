using System;
using DevLib.ModuleSystem;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class PlayerController : NetworkOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        [field:SerializeField] public SettingValueSO SettingValue;
        public IAgentMovement AgentMover { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public ICameraController CameraController { get; private set; }
        
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            AgentMover = GetModule<IAgentMovement>();
            WeaponModule = GetModule<IWeaponModule>();
            CameraController = GetModule<ICameraController>();
        }

        public override void Spawned()
        {
            playerInput.OnAttackKeyPressed += HandleAttack;
        }

        private void HandleAttack(bool isAttack)
        {
            WeaponModule.EquipWeapon(isAttack);
        }

        private void Update()
        {
            if (!HasStateAuthority) return;
            AgentMover.SetMovementDirection(playerInput.MoveDirection);
            CameraController.SetAimDirection(playerInput.AimDirection);
            WeaponModule.SetWeaponMoveDirection(playerInput.WeaponMoveDirection);
        }
    }
}