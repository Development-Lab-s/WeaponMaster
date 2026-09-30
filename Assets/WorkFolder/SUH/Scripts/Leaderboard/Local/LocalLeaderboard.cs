using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Class;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Leaderboard.Local
{
    public class LocalLeaderboard : MonoBehaviour, ILeaderboardGiver, IAfterInitModule
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<ILeaderboardGiver>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<ILeaderboardGiver>(this);
        }

        [SerializeField] private LeaderboardSettingSO leaderboardSettingSO;
        
        private readonly List<LocalLeaderboardEntry> entries = new List<LocalLeaderboardEntry>();
        private readonly object _lock = new object();

        private IDataStoreService service;
        
        public event Action LeaderboardChanged;

        public IReadOnlyList<LocalLeaderboardEntry> GetAllPlayers()
        {
            lock (_lock)
            {
                return entries.ToList();
            }
        }

        public async Task MakePlayer(string playerId)
        {
            await MakePlayer(playerId, playerId);
        }

        public async Task MakePlayer(string playerId, string playerName)
        {
            await UpsertPlayerEntry(playerId, playerName);
        }

        public async Task UpsertPlayerEntry(string playerId, string playerName)
        {
            if (service == null) AfterInit();
            if (service == null) return;

            uint killCount = await service.GetIdAsync<uint>(playerId, nameof(PlayerDataStore.KillCount));
            uint score = await service.GetIdAsync<uint>(playerId, nameof(PlayerDataStore.Score));

            lock (_lock)
            {
                int existingIndex = entries.FindIndex(e => e.PlayerId == playerId);
                var entry = new LocalLeaderboardEntry(playerId, playerName, killCount, score);

                if (existingIndex >= 0)
                {
                    entries[existingIndex] = entry;
                }
                else
                {
                    entries.Add(entry);
                }
            }

            LeaderboardChanged?.Invoke();
        }

        public bool RemovePlayer(string playerId)
        {
            bool dbounce = false;
            lock (_lock)
            {
                int index = entries.FindIndex(e => e.PlayerId == playerId);
                if (index >= 0)
                {
                    entries.RemoveAt(index);
                    dbounce = true;
                }
            }

            if (dbounce)
            {
                LeaderboardChanged?.Invoke();
            }

            return dbounce;
        }

        // 지금 방에 있는 플레이어만 남긴다. 나간 순간에 진행 중이던 갱신이 끝나며 다시 추가된
        // 항목도 이걸로 지워진다.
        public void RetainOnly(HashSet<string> alivePlayerIds)
        {
            bool changed;
            lock (_lock)
            {
                changed = entries.RemoveAll(e => !alivePlayerIds.Contains(e.PlayerId)) > 0;
            }

            if (changed)
            {
                LeaderboardChanged?.Invoke();
            }
        }

        public LocalLeaderboardEntry GetEntry(string playerId)
        {
            lock (_lock)
            {
                return entries.FirstOrDefault(e => e.PlayerId == playerId);
            }
        }

        public bool TryGetEntry(string playerId, out LocalLeaderboardEntry entry)
        {
            lock (_lock)
            {
                entry = entries.FirstOrDefault(e => e.PlayerId == playerId);
                return entry != null;
            }
        }

        public IReadOnlyList<LocalLeaderboardEntry> GetSortedPlayers(int maxCount = -1)
        {
            lock (_lock)
            {
                var query = entries
                    .OrderByDescending(entry => entry.KillCount)
                    .ThenBy(entry => entry.PlayerName);

                if (maxCount > 0)
                {
                    return query.Take(maxCount).ToList();
                }

                return query.ToList();
            }
        }
        
        public void Clear()
        {
            lock (_lock)
            {
                entries.Clear();
            }
            LeaderboardChanged?.Invoke();
        }

        public void AfterInit()
        {
            service = ServiceLocator.Get<IDataStoreService>();
        }
    }
}