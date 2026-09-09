using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.Leaderboard
{
    public class LeaderboardServerInterface : NetworkBehaviour
    {

        [SerializeField] private GameObject template;
        
        private void Start()
        {
            
        }

        private void UpdateLeaderboardUI(
            IReadOnlyList<LeaderboardEntry> topPlayers)
        {
            
        }

        private void OnDisable()
        {
            
        }
    }
}