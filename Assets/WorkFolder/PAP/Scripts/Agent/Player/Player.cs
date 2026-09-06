using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class Player : NetworkOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        [field:SerializeField] public SettingValueSO SettingValue;

        public static List<Player> All { get; private set; }
        public IAgentMovement AgentMover { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public ICameraController CameraController { get; private set; }

        private void Start()
        {
            if (All == null) 
                All = new List<Player>();
            All.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            AgentMover = GetModule<IAgentMovement>();
            WeaponModule = GetModule<IWeaponModule>();
            CameraController = GetModule<ICameraController>();
        }

        public override void Spawned()
        {
            if (!HasInputAuthority) return;
            playerInput.OnAttackKeyPressed += HandleAttack;
        }

        private void HandleAttack(bool isAttack)
        {
            WeaponModule.EquipWeapon(isAttack);
        }

        private void Update()
        {
            if (!HasInputAuthority) return;
            AgentMover.SetMovementDirection(playerInput.MoveDirection);
            CameraController.SetAimDirection(playerInput.AimDirection);
            WeaponModule.SetWeaponMoveDirection(playerInput.WeaponMoveDirection);
        }
    }
}