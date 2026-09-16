using UnityEngine;

namespace WorkFolder.SUH.Scripts.Leaderboard
{
    public class LocalLeaderboardEntry : MonoBehaviour
    {
        public string PlayerId { get; }
        public string PlayerName { get; }
        public uint KillCount { get; }

        public LocalLeaderboardEntry(
            string playerId,
            string playerName,
            uint killCount)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            KillCount = killCount;
        } }
}