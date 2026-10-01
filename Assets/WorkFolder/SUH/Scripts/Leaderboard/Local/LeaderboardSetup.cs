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
        private bool updatingPlayers;
        private float nextPlayersRefreshAt;
        private readonly ServerRequestCache<bool> scoreRequests = new ServerRequestCache<bool>(10);
        
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

        private void OnEnable()
        {
            // Start initializes the interval once; re-enable after death must resume polling.
            if (_wait != null)
                LeaderboardStart();
        }

        private void OnDisable() => LeaderboardStop();

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
                // Restore rank on a newly spawned player even while cloud refresh is pending.
                if (ServiceLocator.Get<ILeaderboardGiver>() is LocalLeaderboard leaderboard)
                    SyncLeaderCrown(leaderboard);
                UpdateAllPlayersData();
                yield return _wait;
            }
        }

        private async void UpdateAllPlayersData()
        {
            if (updatingPlayers || Time.unscaledTime < nextPlayersRefreshAt) return;
            updatingPlayers = true;
            try
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
                if (this == null || !isActiveAndEnabled || localLeaderboard == null) return;

                // 기다리는 동안 나간 플레이어가 있을 수 있어 다시 찾아서 목록을 맞춘다.
                var alivePlayerIds = GameObject.FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(plr => plr.Object != null && plr.Object.IsValid)
                    .Select(plr => plr.playerId.ToString())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();
                localLeaderboard.RetainOnly(alivePlayerIds);

                SyncLeaderCrown(localLeaderboard);
                UpdateLeaderboard();
            }
            catch (Exception ex)
            {
                nextPlayersRefreshAt = Time.unscaledTime + 5f;
                Debug.LogWarning($"Local leaderboard refresh failed; waiting before retry: {ex.Message}");
            }
            finally { updatingPlayers = false; }
        }

        private static void SyncLeaderCrown(LocalLeaderboard leaderboard)
        {
            var players = GameObject.FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var ranked = leaderboard.GetSortedPlayers();
            foreach (var group in players.Where(p => p.Object != null && p.Object.IsValid && p.Runner != null)
                         .GroupBy(p => p.Runner))
            {
                if (!group.Key.IsSharedModeMasterClient) continue;
                var ids = group.Select(p => p.playerId.ToString()).ToHashSet();
                string leaderId = ranked.FirstOrDefault(entry => ids.Contains(entry.PlayerId))?.PlayerId;
                foreach (var player in group)
                {
                    bool leader = !string.IsNullOrEmpty(leaderId) && player.playerId.ToString() == leaderId;
                    // The recipient validates the RPC source, then persists it in Fusion state for late joiners.
                    player.RPC_SetLocalLeaderboardLeader(leader);
                }
            }
        }

        public async void GetPlayerCustomData(string targetPlayerId)
        {
            try
            {
                if (_service == null)
                    _service = ServiceLocator.Get<IDataStoreService>();

                if (_service == null)
                    return;

                await _service.GetIdAsync<uint>(targetPlayerId, nameof(PlayerDataStore.KillCount));
                await _service.GetIdAsync<uint>(targetPlayerId, nameof(PlayerDataStore.Score));
            }
            catch (Exception ex) { Debug.LogWarning($"Player ranking data unavailable: {ex.Message}"); }
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
                    .ThenBy(entry => entry.PlayerName, StringComparer.Ordinal)
                    .ThenBy(entry => entry.PlayerId, StringComparer.Ordinal)
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
            if (!scoreRequests.CanStart) return;
            try
            {
                await scoreRequests.GetOrRun(async () =>
                {
                    await UgsSignIn.EnsureSignedInAsync();
                    var getScoresOptions = new GetScoresOptions
                    {
                        Offset = offset,
                        Limit = limit
                    };

                    var scoresResponse =
                    await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId, getScoresOptions);

                    foreach (var entry in scoresResponse.Results)
                    {
                        var service = ServiceLocator.Get<IDataStoreService>();
                        if (service == null) throw new InvalidOperationException("Player data service is not ready.");
                        // One profile fetch contains both score and kill count.
                        await service.GetIdAsync<uint>(entry.PlayerId, nameof(PlayerDataStore.KillCount));
                    }
                    return true;
                });
            }
            catch (Exception ex) { Debug.LogWarning($"Ranking request failed; retry is throttled: {ex.Message}"); }
        }

        public void AfterInit()
        {
            _service = ServiceLocator.Get<IDataStoreService>();
        }
    }
}
