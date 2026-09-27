using System;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.MainSystems;
using WorkFolder.PAP.Scripts.Network;

namespace WorkFolder.PAP.Scripts.GameSystem
{
    public class StartUIEnable : SimulationBehaviour
    {        
        private ICameraService _service;
        private bool _isFirstTime = true;

        private void Start()
        {
            _service = ServiceLocator.Get<ICameraService>();
            _service.SwitchLobbyCam();
            BK_SoundManger.instance.UpdateBGMSoundState(BGMSoundEnum.EquipmentScene);
        }

        private void OnEnable()
        {
            if (_isFirstTime)
            {
                _isFirstTime = false;
                return;
            }
            _service.SwitchLobbyCam();
            BK_SoundManger.instance.UpdateBGMSoundState(BGMSoundEnum.EquipmentScene);
            NetworkRunner runner = null;

            foreach (NetworkRunner candidate in
                     FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.IsRunning)
                {
                    runner = candidate;
                    break;
                }
            }

            if (runner == null) return;
            
            NetworkObject[] allNetworkObjects = runner.SimulationUnityScene.GetComponents<NetworkObject>(true);

            foreach (var netObj in allNetworkObjects)
            {
                if (netObj.HasStateAuthority)
                {
                    runner.Despawn(netObj);
                }
            }
        }

        private void OnDisable()
        {
            _service.SwitchMainCam();
        }
    }
}