using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ServiceLocator;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Leaderboard.Local
{
    public class LocalLeaderboard : MonoBehaviour, ILeaderboardGiver
    {
        private LeaderboardSettingSO leaderboardSettingSO;
        
        private List<LocalLeaderboardEntry> entries;
        
        public event Action LeaderboardChanged;
        public IReadOnlyList<LocalLeaderboardEntry> GetAllPlayers()
        {
            return entries;
        }

        public async Task MakePlayer(string playerId)
        {
            var keys = new HashSet<string> { nameof(PlayerDataStore.KillCount) };
            var options = new LoadOptions(new PublicReadAccessClassOptions(playerId));

            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys, options);

            if (playerData.TryGetValue(nameof(PlayerDataStore.KillCount), out var item))
            {
                var killCount = item.Value.GetAs<string>();
            }
            
            LeaderboardChanged?.Invoke();
        }

        public void Clear()
        {
            entries.Clear();
            LeaderboardChanged?.Invoke();
        }
    }
}