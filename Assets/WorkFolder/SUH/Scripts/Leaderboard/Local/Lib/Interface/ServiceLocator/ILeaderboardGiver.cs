using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Class;

namespace WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator
{
    public interface ILeaderboardGiver
    {
        event Action LeaderboardChanged;

        IReadOnlyList<LocalLeaderboardEntry> GetAllPlayers();

        Task MakePlayer(string playerId);

        Task UpsertPlayerEntry(string playerId, string playerName);

        
        void Clear();
    }
}