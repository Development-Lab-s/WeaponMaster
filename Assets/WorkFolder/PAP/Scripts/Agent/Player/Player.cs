using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using WorkFolder.PAP.Scripts.Camera;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.PAP.Scripts.WeaponSystem;

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
        
        private bool _canInitialize;
        PartOffsetDataSO _partData; 
        Transform _targetPart;
        private GameObject _weaponPrefab;
        private bool _spawned;

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

        public void SetInitialize(PartOffsetDataSO partData, Transform targetPart, GameObject weaponPrefab)
        {
            _canInitialize = true;
            _partData = partData;
            _targetPart = targetPart;
            _weaponPrefab = weaponPrefab;
        }
        
                
        Transform FindChildHelper(Transform current, string targetName)
        {
            if (current.name == targetName) return current;

            foreach (Transform child in current)
            {
                Transform found = FindChildHelper(child, targetName);
                if (found != null) return found;
            }

            return null;
        }

        public void AttachWeapon()
        {
            WeaponModule.SetPartData(_partData.bodyPartData);
            
            GameObject weapon = Instantiate(_weaponPrefab, WeaponModule.GameObject.transform);
            weapon.transform.localPosition = _partData.offset;
            weapon.transform.localEulerAngles = _partData.eulerAngles;

            Transform rotPart = FindChildHelper(transform,"spine");
            
            ConfigurableJoint joint = rotPart.GetComponent<ConfigurableJoint>();
            if (_partData.driveX == Vector2.zero)
            {
                joint.angularXMotion = ConfigurableJointMotion.Free;
            }
            joint.lowAngularXLimit = new SoftJointLimit()
            {
                limit = _partData.driveX.x
            };
            joint.highAngularXLimit = new SoftJointLimit()
            {
                limit = _partData.driveX.y
            };
            
            weapon.GetComponent<Joint>().connectedBody = _targetPart.GetComponent<Rigidbody>();
            
            WeaponModule.AttachWeapon();
        }

        public override void Spawned()
        {
            _spawned = true;
            if (!HasInputAuthority) return;
            playerInput.OnAttackKeyPressed += HandleAttack;
        }

        public override void Render()
        {
            if (_spawned && _canInitialize)
            {
                _canInitialize = false;
                AttachWeapon();
            }
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