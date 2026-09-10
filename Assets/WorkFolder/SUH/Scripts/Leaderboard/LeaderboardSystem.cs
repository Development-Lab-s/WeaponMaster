using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLoactor;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;

namespace WorkFolder.SUH.Scripts.Leaderboard
{
    public class LeaderboardSystem : MonoBehaviour
    {
        public Action<LeaderboardEntry> OnLeaderboardUpdated;
        
        [SerializeField] private LeaderboardSettingSO leaderboardSettingSO;
        
        private Coroutine _coroutine;
        private WaitForSeconds _wait;
        
        private void Start()
        {
            if (!leaderboardSettingSO)
                return;
            if (leaderboardSettingSO.RefreshIntervalSeconds == null)
                return;
            _wait = new WaitForSeconds(leaderboardSettingSO.RefreshIntervalSeconds);
        }
        
        IEnumerator LeaderboardUpdateState()
        {
            yield return _wait;
        }
        
        private void UpdateLeaderboard()
        {
            ILeaderboardGiver giver =
                ServiceLocator.Get<ILeaderboardGiver>();

            if (giver == null)
                return;

            IReadOnlyList<LeaderboardEntry> allPlayers =
                giver.GetAllPlayers();

            List<LeaderboardEntry> topPlayers =
                allPlayers
                    .OrderByDescending(
                        entry => entry.KillCount
                    )
                    .ThenBy(
                        entry => entry.PlayerName
                    )
                    .Take(
                        leaderboardSettingSO.MaxVisibleRank
                    )
                    .ToList();

            UpdateLeaderboardUI(topPlayers);
        }

        private void UpdateLeaderboardUI(
            IReadOnlyList<LeaderboardEntry> topPlayers)
        {
            
        }
    }
}