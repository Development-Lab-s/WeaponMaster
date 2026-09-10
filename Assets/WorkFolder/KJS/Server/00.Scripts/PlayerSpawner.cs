using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

namespace _00.Scripts
{
    public class PlayerSpawner : SimulationBehaviour , IPlayerJoined
    {

        [SerializeField] private NetworkObject playerPrefab;
        
        public void PlayerJoined(PlayerRef player)
        {
            Debug.Log($"PlayerJoined 호출됨: {player}");

            if (player == Runner.LocalPlayer)
            {
                Debug.Log("내 플레이어 생성 시도");

                Runner.Spawn(playerPrefab , new Vector3(0, 20, 0) , Quaternion.identity);
            }
        }
    
    }
}
