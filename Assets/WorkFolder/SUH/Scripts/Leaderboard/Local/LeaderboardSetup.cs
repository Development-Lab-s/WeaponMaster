using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Leaderboards;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;
using LoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;

namespace WorkFolder.SUH.Scripts.Leaderboard.Local
{
    public class LeaderboardSetup : MonoBehaviour, ILeaderboardState, IAfterInitModule
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<ILeaderboardState>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<ILeaderboardState>(this);
        }

        public Action<LocalLeaderboardEntry> OnLeaderboardUpdated;
        
        [SerializeField] private LeaderboardSettingSO leaderboardSettingSO;

        private string leaderBoardId;
        private ushort offset;
        private ushort limit;
        private Coroutine _coroutine;
        private WaitForSeconds _wait;
        
        private void Start()
        {
            if (!leaderboardSettingSO)
                return;
            if (leaderboardSettingSO.RefreshIntervalSeconds == null)
                return;
            _coroutine = null;
            _wait = new WaitForSeconds(leaderboardSettingSO.RefreshIntervalSeconds);
            leaderBoardId = leaderboardSettingSO.LeaderboardId == null ? "Global_Leaderboard" : leaderboardSettingSO.LeaderboardId;
            offset = (ushort)(leaderboardSettingSO.LeaderboardFindOffset == null ? 0 : leaderboardSettingSO.LeaderboardFindOffset);
            limit = (ushort)(leaderboardSettingSO.MaxVisibleRank == null ? 0 : leaderboardSettingSO.MaxVisibleRank);
        }
        
        public void LeaderboardStart()
        {
            _coroutine = StartCoroutine(LeaderboardUpdateState())!;
        }

        public void LeaderboardStop()
        {
            StopCoroutine(_coroutine!);
            _coroutine = null;
        }

        IEnumerator LeaderboardUpdateState()
        {
            yield return _wait;
            UpdateLeaderboard();
        }
        
        private void UpdateLeaderboard()
        {
            ILeaderboardGiver giver =
                ServiceLocator.Get<ILeaderboardGiver>();

            if (giver == null)
                return;

            IReadOnlyList<LocalLeaderboardEntry> allPlayers =
                giver.GetAllPlayers();

            List<LocalLeaderboardEntry> topPlayers =
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
            IReadOnlyList<LocalLeaderboardEntry> topPlayers)
        {
            foreach (var plr in topPlayers)
            {
                Debug.Log(plr.PlayerName);
            }
        }
        
        public async void AddScore(double score)
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderBoardId, score);
        }

        public async void GetScores()
        {
            var getScoresOptions = new GetScoresOptions
            {
                Offset = offset,
                Limit = limit
            };

            var scoresResponse =
                await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId, getScoresOptions);

            foreach (var entry in scoresResponse.Results)
            {
                GetPlayerCustomData(entry.PlayerId);
            }
        }

        public async void GetPlayerCustomData(string targetPlayerId)
        {

        }

        public void AfterInit()
        {

        }
    }
}