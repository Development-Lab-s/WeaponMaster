using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.PAP.Scripts.WeaponSystem;

namespace WorkFolder.PAP.Scripts.SelectUIScene
{
    public class PlayerSpawner : SimulationBehaviour
    {
        [SerializeField] private EquipSocketContainer container;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private PartOffsetDataSO[] partDataList;
        [SerializeField] private Transform[] playerSpawnPoints;
        
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
        
        public void SpawnCharacter()
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
            (int, ItemData) pack = container.GetCurrentPartAndWeapon();
            NetworkObject player = runner.Spawn(playerPrefab, playerSpawnPoints[Random.Range(0,playerSpawnPoints.Length)].position + new Vector3(Random.Range(-3f,3f),0,Random.Range(-3f,3f)),Quaternion.identity,inputAuthority:runner.LocalPlayer);
            runner.SetPlayerObject(runner.LocalPlayer, player);
            Player playerHandler = player.GetComponent<Player>();
            PartOffsetDataSO partData = partDataList[pack.Item1];
            playerHandler.SetInitialize(partData,FindChildHelper(player.transform,partData.targetPartName).transform.GetComponent<NetworkObject>(),pack.Item2.ItemPrefab);
        }
    }
}