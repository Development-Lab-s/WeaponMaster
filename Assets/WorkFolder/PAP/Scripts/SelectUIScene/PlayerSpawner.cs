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
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private PartOffsetDataSO[] partDataList;
        
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
            (int, ItemData) pack = container.GetCurrentPartAndWeapon();
            
            NetworkObject player = RunnerProvider.Current.Spawn(playerPrefab, new Vector3(0,0,0),Quaternion.identity,inputAuthority:RunnerProvider.Current.LocalPlayer);
            Player playerHandler = player.GetComponent<Player>();
            PartOffsetDataSO partData = partDataList[pack.Item1];
            playerHandler.SetInitialize(partData,FindChildHelper(player.transform,partData.targetPartName).transform.GetComponent<NetworkObject>(),pack.Item2.ItemPrefab);
        }
    }
}