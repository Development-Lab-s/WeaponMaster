using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Unity.Services.Leaderboards;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.SUH.Scripts.DataStoreService;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Class;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

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
            LeaderboardStop();
            DevLib.ServiceLocator.ServiceLocator.UnRegister<ILeaderboardState>(this);
        }

        public Action<LocalLeaderboardEntry> OnLeaderboardUpdated;
        public Action<IReadOnlyList<LocalLeaderboardEntry>> OnLeaderboardListUpdated;
        
        [SerializeField] private LeaderboardSettingSO leaderboardSettingSO;
        [SerializeField] private RankingRow[] rankingRow; 
        
        
        private string leaderBoardId = "Global_Leaderboard";
        private ushort offset = 0;
        private ushort limit = 10;
        private Coroutine _coroutine;
        private WaitForSeconds _wait;
        private IDataStoreService _service;
        
        private void Start()
        {
            float interval = 1f;

            if (leaderboardSettingSO != null)
            {
                if (!string.IsNullOrEmpty(leaderboardSettingSO.LeaderboardId))
                    leaderBoardId = leaderboardSettingSO.LeaderboardId;

                offset = (ushort)leaderboardSettingSO.LeaderboardFindOffset;
                limit = (ushort)leaderboardSettingSO.MaxVisibleRank;

                if (leaderboardSettingSO.RefreshIntervalSeconds > 0)
                    interval = leaderboardSettingSO.RefreshIntervalSeconds;
            }

            _wait = new WaitForSeconds(interval);

            LeaderboardStart();
        }

        public void LeaderboardStart()
        {
            if (_coroutine != null)
                LeaderboardStop();

            _coroutine = StartCoroutine(LeaderboardUpdateState());
        }

        public void LeaderboardStop()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private IEnumerator LeaderboardUpdateState()
        {
            while (true)
            {
                UpdateAllPlayersData();
                yield return _wait;
            }
        }

        private async void UpdateAllPlayersData()
        {
            var players = GameObject.FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var localLeaderboard = ServiceLocator.Get<ILeaderboardGiver>() as LocalLeaderboard;

            if (localLeaderboard == null) return;

            var tasks = players
                .Where(plr => plr.Object != null && plr.Object.IsValid)
                .Select(plr => (id: plr.playerId.ToString(), name: plr.playerName.ToString()))
                .Where(p => !string.IsNullOrEmpty(p.id))
                .Select(p => localLeaderboard.UpsertPlayerEntry(p.id, p.name))
                .ToList();

            await Task.WhenAll(tasks);

            // 기다리는 동안 나간 플레이어가 있을 수 있어 다시 찾아서 목록을 맞춘다.
            var alivePlayerIds = GameObject.FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(plr => plr.Object != null && plr.Object.IsValid)
                .Select(plr => plr.playerId.ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();
            localLeaderboard.RetainOnly(alivePlayerIds);

            UpdateLeaderboard();
        }

        public async void GetPlayerCustomData(string targetPlayerId)
        {
            if (_service == null)
                _service = ServiceLocator.Get<IDataStoreService>();

            if (_service == null)
                return;

            await _service.GetIdAsync<uint>(targetPlayerId, nameof(PlayerDataStore.KillCount));
            await _service.GetIdAsync<uint>(targetPlayerId, nameof(PlayerDataStore.Score));
        }

        [ContextMenu("UpdateLeaderboardUI")]
        private void UpdateLeaderboard()
        {
            ILeaderboardGiver giver = ServiceLocator.Get<ILeaderboardGiver>();

            if (giver == null)
                return;

            IReadOnlyList<LocalLeaderboardEntry> allPlayers = giver.GetAllPlayers();

            if (allPlayers == null)
                return;

            int takeCount = (leaderboardSettingSO != null && leaderboardSettingSO.MaxVisibleRank > 0)
                ? leaderboardSettingSO.MaxVisibleRank
                : limit;

            List<LocalLeaderboardEntry> topPlayers =
                allPlayers
                    .OrderByDescending(entry => entry.KillCount)
                    .ThenBy(entry => entry.PlayerName)
                    .Take(takeCount)
                    .ToList();
            UpdateLeaderboardUI(topPlayers);
        }

        private async void UpdateLeaderboardUI(IReadOnlyList<LocalLeaderboardEntry> topPlayers)
        {
            ushort sum = 0;
            foreach (var entry in rankingRow)
            {
                if (sum > 5) break;
                entry.SetData(++sum,"",0,0);
            }

            sum = 0;
            foreach (var plr in topPlayers)
            {
                if (sum > 4) break;
                rankingRow[sum].SetData(++sum, plr.PlayerName, Convert.ToInt32(plr.Score), Convert.ToInt32(plr.KillCount));
            }
            if (_service == null)
                _service = ServiceLocator.Get<IDataStoreService>();
            string playerName = await _service.GetAsync<string>(nameof(PlayerDataStore.PlayerName));
            uint killCount = await _service.GetAsync<uint>(nameof(PlayerDataStore.KillCount));
            uint score = await _service.GetAsync<uint>(nameof(PlayerDataStore.Score));

            rankingRow[4].SetData(0, playerName,(int)score ,(int)killCount);
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

        public void AfterInit()
        {
            _service = ServiceLocator.Get<IDataStoreService>();
        }
    }
}