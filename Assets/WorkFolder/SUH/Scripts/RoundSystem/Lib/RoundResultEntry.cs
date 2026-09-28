using System;

namespace WorkFolder.SUH.Scripts.RoundSystem.Lib
{
    [Serializable]
    public sealed class RoundResultEntry
    {
        public string PlayerId { get; }
        public string PlayerName { get; }
        public int Score { get; }
        public int KillCount { get; }

        public RoundResultEntry(string playerId, string playerName, int score, int killCount)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
            KillCount = killCount;
        }
    }
}
