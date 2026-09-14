using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator
{
    public interface ILeaderboardGiver
    {
        event Action LeaderboardChanged;

        IReadOnlyList<LocalLeaderboardEntry> GetAllPlayers();

        Task MakePlayer(string playerId);

        
        void Clear();
    }
}