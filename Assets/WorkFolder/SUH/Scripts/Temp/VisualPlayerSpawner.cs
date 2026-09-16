using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.SUH.Scripts.Temp
{
    public class VisualPlayerSpawner : SimulationBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        
        public void SpawnCharacter()
        {
            NetworkObject player = RunnerProvider.Current.Spawn(playerPrefab, new Vector3(0,4,0),Quaternion.identity,inputAuthority:RunnerProvider.Current.LocalPlayer);
            Player playerHandler = player.GetComponent<Player>();
        }
    }
}