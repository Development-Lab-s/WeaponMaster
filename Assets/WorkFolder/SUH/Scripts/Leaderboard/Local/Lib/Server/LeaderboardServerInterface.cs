using System.Collections.Generic;
using Fusion;
using UnityEngine;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Class;

namespace WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.Server
{
    public class LeaderboardServerInterface : NetworkBehaviour
    {

        [SerializeField] private GameObject template;
        
        private void Start()
        {
            
        }

        private void UpdateLeaderboardUI(
            IReadOnlyList<LocalLeaderboardEntry> topPlayers)
        {
            
        }

        private void OnDisable()
        {
            
        }
    }
}