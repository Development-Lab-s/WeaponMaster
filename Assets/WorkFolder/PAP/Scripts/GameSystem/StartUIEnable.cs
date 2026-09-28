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
        [SerializeField] private GameObject targetEnable;

        private void Start()
        {
            _service = ServiceLocator.Get<ICameraService>();
            _service.SwitchLobbyCam();
            BK_SoundManger.instance.UpdateBGMSoundState(BGMSoundEnum.EquipmentScene);

        }

        private void OnEnable()
        {
            targetEnable.SetActive(false);
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

            // 죽은 내 캐릭터·무기처럼 내가 스폰한 것만 치운다. 공유 모드에서 씬에 배치된
            // NetworkObject(EffectManager, 킬로그 등)는 마스터가 권한을 가져서, 이 조건이 없으면
            // 마스터가 한 번 죽을 때 씬 오브젝트까지 디스폰돼 그 뒤 RPC가 전부 깨진다.
            // 중첩 오브젝트(퍼펫의 머리·척추)는 부모와 함께 디스폰되므로 따로 부르지 않는다.
            foreach (var netObj in allNetworkObjects)
            {
                if (netObj.HasStateAuthority
                    && !netObj.NetworkTypeId.IsSceneObject
                    && !netObj.IsNested)
                {
                    runner.Despawn(netObj);
                }
            }
        }

        private void OnDisable()
        {
            targetEnable.SetActive(true);
            _service.SwitchMainCam();
        }
    }
}