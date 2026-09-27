using System;

namespace WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Class
{
    [Serializable]
    public class LocalLeaderboardEntry
    {
        public string PlayerId { get; }
        public string PlayerName { get; }
        public uint KillCount { get; }
        
        public uint Score { get;  }

        public LocalLeaderboardEntry(
            string playerId,
            string playerName,
            uint killCount,
            uint score)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            KillCount = killCount;
            Score = score;
        } }
}