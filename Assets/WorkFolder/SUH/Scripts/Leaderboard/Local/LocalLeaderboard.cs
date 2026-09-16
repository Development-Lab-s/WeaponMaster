using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
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

        private LeaderboardSettingSO leaderboardSettingSO;
        
        private readonly List<LocalLeaderboardEntry> entries = new List<LocalLeaderboardEntry>();

        private IDataStoreService service;
        
        public event Action LeaderboardChanged;
        public IReadOnlyList<LocalLeaderboardEntry> GetAllPlayers()
        {
            return entries;
        }

        public async Task MakePlayer(string playerId)
        {
            AfterInit();
            if (service == null) return;
            await service.GetIdAsync<uint>(playerId, nameof(PlayerDataStore.KillCount));
            
            LeaderboardChanged?.Invoke();
        }

        public void Clear()
        {
            entries.Clear();
            LeaderboardChanged?.Invoke();
        }

        public void AfterInit()
        {
            service = ServiceLocator.Get<IDataStoreService>();
        }
    }
}