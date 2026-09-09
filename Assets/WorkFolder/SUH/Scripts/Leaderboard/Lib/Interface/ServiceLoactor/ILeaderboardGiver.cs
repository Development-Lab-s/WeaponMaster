using System;
using System.Collections.Generic;

namespace WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLoactor
{
    public interface ILeaderboardGiver
    {
        event Action LeaderboardChanged;

        IReadOnlyList<LeaderboardEntry> GetAllPlayers();

        bool TryGetPlayer(
            string playerId,
            out LeaderboardEntry entry
        );


        void Clear();
    }
}