using System;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Network;

namespace WorkFolder.PAP.Scripts.GameSystem
{
    public class StartUIEnable : SimulationBehaviour
    {

        private void OnEnable()
        {
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
            BK_SoundManger.instance.UpdateBGMSoundState(BGMSoundEnum.EquipmentScene);
            
            NetworkObject[] allNetworkObjects = runner.SimulationUnityScene.GetComponents<NetworkObject>(true);

            foreach (var netObj in allNetworkObjects)
            {
                if (netObj.HasStateAuthority)
                {
                    runner.Despawn(netObj);
                }
            }
        }
    }
}