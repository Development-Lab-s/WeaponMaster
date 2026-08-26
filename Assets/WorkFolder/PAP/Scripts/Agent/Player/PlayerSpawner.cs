using Fusion;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
    {
        public GameObject PlayerPrefab;
    
        public void PlayerJoined(PlayerRef player)
        {
            if (player == Runner.LocalPlayer)
            {
                Debug.Log("Player joined");
                Runner.Spawn(PlayerPrefab, new Vector3(0,4,0),Quaternion.identity);
            }
        }
    }
}
