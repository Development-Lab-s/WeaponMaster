using UnityEngine;

namespace WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject
{
    [CreateAssetMenu(fileName = "leaderboardSetting data", menuName = "SO/Suxghui/Leaderboard/Setting SO", order = 0)]
    public class LeaderboardSettingSO : UnityEngine.ScriptableObject
    {
        public int MaxVisibleRank;
        public float RefreshIntervalSeconds;
    }
}