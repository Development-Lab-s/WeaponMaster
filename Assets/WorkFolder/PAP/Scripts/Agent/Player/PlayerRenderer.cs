using System;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Camera;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class PlayerRenderer : NetworkModule
    {
        [SerializeField] private Transform waistTrm;
        private ICameraController _cameraController;

        private void Start()
        {
            _cameraController = Owner.GetModule<ICameraController>();
        }

        public override void Render()
        {
            if (!HasStateAuthority || _cameraController == null) return;
            waistTrm.eulerAngles = _cameraController.EularAngle;
        }
    }
}