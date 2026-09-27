 using System;
using DevLib.ServiceLocator;
using Fusion;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.PAP.Scripts.MainSystems;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.SUH.Scripts.DataStoreService;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

namespace WorkFolder.PAP.Scripts.GameSystem
{
    public class StartUIEnable : SimulationBehaviour
    {        
        private ICameraService _service;
        private bool _isFirstTime = true;
        NetworkRunner runner = null;

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